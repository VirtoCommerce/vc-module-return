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

    // Answers whether the URLs could be attached, without writing anything, so a create can refuse
    // before it commits a draft the buyer never gets told about.
    Task ValidateAvailable(Return orderReturn, IEnumerable<string> urls);

    Task SaveFiles(IList<File> files);

    // Deletes the released files no return refers to any more. Deleted rather than unowned: an
    // unowned file is readable and deletable by any authenticated caller in this scope. Best effort -
    // the return is already committed by the time this runs.
    Task DeleteUnreferencedFiles(IList<File> files);
}
