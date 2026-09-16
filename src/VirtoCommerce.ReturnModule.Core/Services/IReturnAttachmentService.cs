using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

public interface IReturnAttachmentService
{
    Task UpdateAttachmentsAsync(Return orderReturn, ReturnLineItem lineItem, IList<string> urls);
}
