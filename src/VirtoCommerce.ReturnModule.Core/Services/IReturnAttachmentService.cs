using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

public interface IReturnAttachmentService
{
    // Returns the files whose ownership has to change, without writing them: the caller saves the
    // return first, so a failure there leaves file ownership untouched.
    Task<IList<File>> UpdateAttachmentsAsync(Return orderReturn, ReturnLineItem lineItem, IList<string> urls);

    Task SaveFilesAsync(IList<File> files);
}
