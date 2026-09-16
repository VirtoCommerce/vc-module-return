using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

public interface IReturnAttachmentService
{
    // Reports what has to change without writing it: the caller saves the return first, so a
    // failure there leaves file ownership untouched.
    Task<ReturnAttachmentChanges> UpdateAttachments(Return orderReturn, ReturnLineItem lineItem, IList<string> urls);

    Task SaveFiles(IList<File> files);

    // A file no line of the return refers to any more. Deleted rather than unowned: an unowned file
    // is readable and deletable by any authenticated caller in this scope.
    Task DeleteFiles(IList<File> files);
}
