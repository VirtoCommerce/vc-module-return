using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using VirtoCommerce.FileExperienceApi.Core.Authorization;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.ReturnModule.Core;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Authorization;

/// <summary>
/// Guards the return attachments scope: a file there may only be uploaded or read by a signed-in
/// buyer.
/// </summary>
/// <remarks>
/// Attaching a file to a return is guarded separately and more tightly — the flow service only
/// claims files that are unowned or already this return's, and only the return's owner can edit it.
/// </remarks>
public class ReturnFileAuthorizationRequirementFactory : IFileAuthorizationRequirementFactory
{
    public string Scope => ModuleConstants.ReturnAttachmentsScope;

    public IAuthorizationRequirement Create(File file, string permission)
    {
        return new DenyAnonymousAuthorizationRequirement();
    }
}
