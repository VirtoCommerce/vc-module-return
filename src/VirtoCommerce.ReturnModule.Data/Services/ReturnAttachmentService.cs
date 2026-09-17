using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VirtoCommerce.AssetsModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Extensions;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Repositories;

namespace VirtoCommerce.ReturnModule.Data.Services;

public class ReturnAttachmentService : IReturnAttachmentService
{
    private const string _urlPrefix = "/api/files/";
    private static readonly StringComparer _ignoreCase = StringComparer.OrdinalIgnoreCase;

    private readonly IFileUploadService _fileUploadService;
    private readonly IAssetEntryService _assetEntryService;
    private readonly IUserNameResolver _userNameResolver;
    private readonly Func<IReturnRepository> _repositoryFactory;
    private readonly ILogger<ReturnAttachmentService> _logger;

    public ReturnAttachmentService(
        IFileUploadService fileUploadService,
        IAssetEntryService assetEntryService,
        IUserNameResolver userNameResolver,
        Func<IReturnRepository> repositoryFactory,
        ILogger<ReturnAttachmentService> logger)
    {
        _fileUploadService = fileUploadService;
        _assetEntryService = assetEntryService;
        _userNameResolver = userNameResolver;
        _repositoryFactory = repositoryFactory;
        _logger = logger;
    }

    public virtual Task<ReturnAttachmentChanges> UpdateAttachments(Return orderReturn, ReturnLineItem lineItem, IList<string> urls)
    {
        ArgumentNullException.ThrowIfNull(orderReturn);
        ArgumentNullException.ThrowIfNull(lineItem);

        return UpdateAttachmentsInternal(orderReturn, lineItem, urls);
    }

    public virtual Task ValidateAvailable(Return orderReturn, IEnumerable<string> urls)
    {
        ArgumentNullException.ThrowIfNull(orderReturn);

        return ValidateAvailableInternal(orderReturn, urls);
    }

    protected virtual async Task ValidateAvailableInternal(Return orderReturn, IEnumerable<string> urls)
    {
        var wanted = (urls ?? []).Distinct(_ignoreCase).ToList();

        if (wanted.Count == 0)
        {
            return;
        }

        var available = await GetAttachableFilesAsync(orderReturn, wanted);
        var missing = wanted.Find(x => !available.ContainsKey(x));

        if (missing != null)
        {
            throw NotAvailable(missing);
        }
    }

    protected virtual async Task<ReturnAttachmentChanges> UpdateAttachmentsInternal(Return orderReturn, ReturnLineItem lineItem, IList<string> urls)
    {
        lineItem.Attachments ??= [];

        var wanted = urls ?? [];
        var current = lineItem.Attachments.Select(x => x.Url).ToList();

        var filesByUrl = await GetAttachableFilesAsync(orderReturn, wanted.Concat(current));
        var changes = AbstractTypeFactory<ReturnAttachmentChanges>.TryCreateInstance();

        foreach (var attachment in lineItem.Attachments.Where(x => !wanted.Contains(x.Url, _ignoreCase)).ToList())
        {
            lineItem.Attachments.Remove(attachment);

            // Ownership is left alone here: another line of the same return may still show this
            // file, and only the caller can see all of them.
            if (filesByUrl.TryGetValue(attachment.Url, out var file))
            {
                changes.Released.Add(file);
            }
        }

        foreach (var url in wanted.Except(current, _ignoreCase))
        {
            // Missing, in another scope, uploaded by somebody else, or owned by another return.
            // Dropping it quietly answers 200 with a line that has no attachment, and the buyer's
            // next signal is ATTACHMENTS_REQUIRED at submit, which points at the wrong thing.
            if (!filesByUrl.TryGetValue(url, out var file))
            {
                throw NotAvailable(url);
            }

            lineItem.Attachments.Add(ToAttachment(file));

            file.OwnerEntityId = orderReturn.Id;
            file.OwnerEntityType = nameof(Return);
            changes.Claimed.Add(file);
        }

        return changes;
    }

    public virtual async Task SaveFiles(IList<File> files)
    {
        if (files?.Count > 0)
        {
            await _fileUploadService.SaveChangesAsync(files);
        }
    }

    public virtual async Task DeleteUnreferencedFiles(IList<File> files)
    {
        var candidates = (files ?? []).DistinctBy(x => x.Id, _ignoreCase).ToList();

        if (candidates.Count == 0)
        {
            return;
        }

        var urls = candidates.Select(x => GetUrl(x.Id)).ToList();

        // Asked of the database rather than of the return in hand: the same file may hang off
        // another line, or another draft of the same buyer, and only the table knows about those.
        // The return is already saved at this point, so its own dropped rows are gone from it too.
        using var repository = _repositoryFactory();

        var stillUsed = await repository.ReturnAttachments
            .Where(x => urls.Contains(x.Url))
            .Select(x => x.Url)
            .Distinct()
            .ToListAsync();

        var orphaned = candidates
            .Where(x => !stillUsed.Contains(GetUrl(x.Id), _ignoreCase))
            .Select(x => x.Id)
            .ToList();

        if (orphaned.Count == 0)
        {
            return;
        }

        try
        {
            await _fileUploadService.DeleteAsync(orphaned);
        }
        catch (Exception ex)
        {
            // The mutation it belongs to has already been committed, so reporting a failure here
            // would deny a change the buyer can see. The files stay owned by a return that no longer
            // refers to them and need reclaiming out of band.
            _logger.LogError(ex, "Failed to delete attachments {FileIds} released by return {ReturnId}.",
                string.Join(", ", orphaned), candidates[0].OwnerEntityId);
        }
    }

    // A file may be attached when it is already this return's, or when it is unclaimed and this
    // buyer is the one who uploaded it. Without the second half any authenticated caller could name
    // a stranger's unattached upload, and - since a dropped file is now deleted - destroy it.
    protected virtual async Task<IDictionary<string, File>> GetAttachableFilesAsync(Return orderReturn, IEnumerable<string> urls)
    {
        var files = (await GetFilesAsync(urls))
            .Where(x => x.Scope.EqualsIgnoreCase(ModuleConstants.ReturnAttachmentsScope))
            .ToList();

        var unclaimed = files.Where(x => x.OwnerIsEmpty()).Select(x => x.Id).ToList();
        IList<string> ownUploads = unclaimed.Count == 0 ? [] : await GetOwnUploadsAsync(unclaimed);

        return files
            .Where(x => IsOwnedBy(x, orderReturn) || ownUploads.Contains(x.Id, _ignoreCase))
            .ToDictionary(x => GetUrl(x.Id), _ignoreCase);
    }

    // The upload itself carries no author, but the asset behind it does - it is an AuditableEntity,
    // stamped by the platform from the same resolver this reads.
    protected virtual async Task<IList<string>> GetOwnUploadsAsync(IList<string> fileIds)
    {
        var userName = _userNameResolver.GetCurrentUserName();

        if (string.IsNullOrEmpty(userName))
        {
            return [];
        }

        var assets = await _assetEntryService.GetNoCloneAsync(fileIds);

        return assets
            .Where(x => x.CreatedBy.EqualsIgnoreCase(userName))
            .Select(x => x.Id)
            .ToList();
    }

    protected static bool IsOwnedBy(File file, Return orderReturn)
    {
        return !string.IsNullOrEmpty(orderReturn.Id) &&
               file.OwnerEntityType.EqualsIgnoreCase(nameof(Return)) &&
               file.OwnerEntityId.EqualsIgnoreCase(orderReturn.Id);
    }

    protected virtual Task<IList<File>> GetFilesAsync(IEnumerable<string> urls)
    {
        var ids = urls
            .Distinct(_ignoreCase)
            .Select(GetId)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        return ids.Count == 0
            ? Task.FromResult<IList<File>>([])
            : _fileUploadService.GetAsync(ids);
    }

    protected virtual ReturnAttachment ToAttachment(File file)
    {
        var result = AbstractTypeFactory<ReturnAttachment>.TryCreateInstance();

        result.Name = file.Name;
        result.Url = GetUrl(file.Id);
        result.MimeType = file.ContentType;
        result.Size = file.Size;

        return result;
    }

    protected static ReturnFlowException NotAvailable(string url)
    {
        return new ReturnFlowException(
            ReturnFlowError.AttachmentNotAvailable,
            $"File '{url}' is not available to attach to this return.");
    }

    protected static string GetUrl(string id)
    {
        return $"{_urlPrefix}{id}";
    }

    protected static string GetId(string url)
    {
        return url != null && url.StartsWith(_urlPrefix, StringComparison.OrdinalIgnoreCase)
            ? url[_urlPrefix.Length..]
            : null;
    }
}
