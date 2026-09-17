using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable;
using Moq;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.AssetsModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Data.Models;
using VirtoCommerce.ReturnModule.Data.Repositories;
using VirtoCommerce.ReturnModule.Data.Services;
using Xunit;
using File = VirtoCommerce.FileExperienceApi.Core.Models.File;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnAttachmentServiceTests
{
    private const string Buyer = "buyer@example.com";
    private const string Stranger = "someone-else@example.com";
    private const string FileId = "file-1";
    private const string Url = "/api/files/file-1";

    [Fact]
    public async Task OwnUnclaimedUpload_IsClaimed()
    {
        var context = new Context(MakeFile(FileId), MakeAsset(FileId, Buyer));

        var changes = await context.Attach(Url);

        Assert.Equal(FileId, Assert.Single(changes.Claimed).Id);
        Assert.Equal("return-1", changes.Claimed[0].OwnerEntityId);
        Assert.Equal(nameof(Return), changes.Claimed[0].OwnerEntityType);
    }

    [Fact]
    public async Task UnclaimedUploadOfAnotherUser_IsRefused()
    {
        // Claiming is followed by a delete once the line is dropped, so admitting a stranger's file
        // here would destroy it.
        var context = new Context(MakeFile(FileId), MakeAsset(FileId, Stranger));

        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() => context.Attach(Url));

        Assert.Equal(ReturnFlowError.AttachmentNotAvailable, exception.Code);
    }

    [Fact]
    public async Task FileOwnedByAnotherReturn_IsRefused()
    {
        var file = MakeFile(FileId);
        file.OwnerEntityId = "return-2";
        file.OwnerEntityType = nameof(Return);

        var context = new Context(file, MakeAsset(FileId, Buyer));

        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() => context.Attach(Url));

        Assert.Equal(ReturnFlowError.AttachmentNotAvailable, exception.Code);
    }

    [Fact]
    public async Task FileOwnedByThisReturn_NeedsNoUploaderLookup()
    {
        var file = MakeFile(FileId);
        file.OwnerEntityId = "return-1";
        file.OwnerEntityType = nameof(Return);

        // The uploader is a stranger: an already-claimed file is this return's regardless, and the
        // asset store must not be asked about it.
        var context = new Context(file, MakeAsset(FileId, Stranger));

        var changes = await context.Attach(Url);

        Assert.Single(changes.Claimed);
        context.AssetEntryService.Verify(
            x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task FileInAnotherScope_IsRefused()
    {
        var file = MakeFile(FileId);
        file.Scope = "avatars";

        var context = new Context(file, MakeAsset(FileId, Buyer));

        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() => context.Attach(Url));

        Assert.Equal(ReturnFlowError.AttachmentNotAvailable, exception.Code);
    }

    [Fact]
    public async Task ReleasedFile_StillOnAnotherReturn_IsKept()
    {
        // The other return is another draft of the same buyer - deciding from this return's lines
        // alone would delete a file that draft still renders.
        var context = new Context(MakeFile(FileId), MakeAsset(FileId, Buyer), attachmentRows: [Url]);

        await context.Service.DeleteUnreferencedFiles([MakeFile(FileId)]);

        context.FileUploadService.Verify(x => x.DeleteAsync(It.IsAny<IList<string>>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task ReleasedFile_ReferencedByNothing_IsDeleted()
    {
        var context = new Context(MakeFile(FileId), MakeAsset(FileId, Buyer));

        await context.Service.DeleteUnreferencedFiles([MakeFile(FileId)]);

        context.FileUploadService.Verify(
            x => x.DeleteAsync(It.Is<IList<string>>(ids => ids.Contains(FileId)), It.IsAny<bool>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteFailure_IsNotReportedToTheCaller()
    {
        // The return it belongs to is already committed by the time this runs.
        var context = new Context(MakeFile(FileId), MakeAsset(FileId, Buyer));
        context.FileUploadService
            .Setup(x => x.DeleteAsync(It.IsAny<IList<string>>(), It.IsAny<bool>()))
            .ThrowsAsync(new InvalidOperationException("blob store is down"));

        await context.Service.DeleteUnreferencedFiles([MakeFile(FileId)]);
    }

    private static File MakeFile(string id)
    {
        return new File
        {
            Id = id,
            Name = "photo.jpg",
            ContentType = "image/jpeg",
            Size = 1024,
            Scope = ModuleConstants.ReturnAttachmentsScope,
        };
    }

    private static AssetEntry MakeAsset(string id, string createdBy)
    {
        return new AssetEntry { Id = id, CreatedBy = createdBy };
    }

    private sealed class Context
    {
        public Mock<IFileUploadService> FileUploadService { get; } = new();

        public Mock<IAssetEntryService> AssetEntryService { get; } = new();

        public ReturnAttachmentService Service { get; }

        public Context(File file, AssetEntry asset, IList<string> attachmentRows = null)
        {
            FileUploadService
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync((IList<string> ids, string _, bool _) =>
                    ids.Contains(file.Id) ? [file] : []);

            AssetEntryService
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync((IList<string> ids, string _, bool _) =>
                    ids.Contains(asset.Id) ? [asset] : []);

            var userNameResolver = new Mock<IUserNameResolver>();
            userNameResolver.Setup(x => x.GetCurrentUserName()).Returns(Buyer);

            var rows = (attachmentRows ?? [])
                .Select(x => new ReturnAttachmentEntity { Url = x })
                .ToList()
                .BuildMock();

            var repository = new Mock<IReturnRepository>();
            repository.Setup(x => x.ReturnAttachments).Returns(rows);

            Service = new ReturnAttachmentService(
                FileUploadService.Object,
                AssetEntryService.Object,
                userNameResolver.Object,
                () => repository.Object,
                NullLogger<ReturnAttachmentService>.Instance);
        }

        public Task<ReturnAttachmentChanges> Attach(string url)
        {
            var orderReturn = new Return { Id = "return-1" };
            var lineItem = new ReturnLineItem();

            return Service.UpdateAttachments(orderReturn, lineItem, [url]);
        }
    }
}
