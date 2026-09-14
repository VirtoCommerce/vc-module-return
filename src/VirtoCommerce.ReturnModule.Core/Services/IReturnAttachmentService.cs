using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

public interface IReturnAttachmentService
{
    /// <summary>
    /// Makes the line's attachments match <paramref name="urls"/> exactly: claims the new files and
    /// releases the ones the buyer removed.
    /// </summary>
    Task UpdateAttachmentsAsync(Return orderReturn, ReturnLineItem lineItem, IList<string> urls);
}
