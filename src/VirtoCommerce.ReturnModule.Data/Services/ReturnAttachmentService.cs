using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.FileExperienceApi.Core.Extensions;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ReturnModule.Data.Services;

public class ReturnAttachmentService : IReturnAttachmentService
{
    private const string _urlPrefix = "/api/files/";
    private static readonly StringComparer _ignoreCase = StringComparer.OrdinalIgnoreCase;

    private readonly IFileUploadService _fileUploadService;

    public ReturnAttachmentService(IFileUploadService fileUploadService)
    {
        _fileUploadService = fileUploadService;
    }

    public virtual async Task<IList<File>> UpdateAttachmentsAsync(Return orderReturn, ReturnLineItem lineItem, IList<string> urls)
    {
        ArgumentNullException.ThrowIfNull(orderReturn);
        ArgumentNullException.ThrowIfNull(lineItem);

        lineItem.Attachments ??= [];

        var wanted = urls ?? [];
        var current = lineItem.Attachments.Select(x => x.Url).ToList();

        var files = await GetFilesAsync(wanted.Concat(current));
        var changedFiles = new List<File>();

        var filesByUrl = files
            .Where(x => x.Scope.EqualsIgnoreCase(ModuleConstants.ReturnAttachmentsScope) &&
                        (x.OwnerIsEmpty() || IsOwnedBy(x, orderReturn)))
            .ToDictionary(x => GetUrl(x.Id), _ignoreCase);

        foreach (var attachment in lineItem.Attachments.Where(x => !wanted.Contains(x.Url, _ignoreCase)).ToList())
        {
            lineItem.Attachments.Remove(attachment);

            if (filesByUrl.TryGetValue(attachment.Url, out var file))
            {
                file.OwnerEntityId = null;
                file.OwnerEntityType = null;
                changedFiles.Add(file);
            }
        }

        foreach (var url in wanted.Except(current, _ignoreCase))
        {
            if (!filesByUrl.TryGetValue(url, out var file))
            {
                continue;
            }

            lineItem.Attachments.Add(ToAttachment(file));

            file.OwnerEntityId = orderReturn.Id;
            file.OwnerEntityType = nameof(Return);
            changedFiles.Add(file);
        }

        return changedFiles;
    }

    public virtual async Task SaveFilesAsync(IList<File> files)
    {
        if (files?.Count > 0)
        {
            await _fileUploadService.SaveChangesAsync(files);
        }
    }

    protected static bool IsOwnedBy(File file, Return orderReturn)
    {
        return file.OwnerEntityType.EqualsIgnoreCase(nameof(Return)) &&
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
