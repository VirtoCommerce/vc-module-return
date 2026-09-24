using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.FileExperienceApi.Core.Authorization;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.ReturnModule.Core;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Authorization;

public class ReturnFileAuthorizationRequirementFactory : IFileAuthorizationRequirementFactory
{
    public string Scope => ModuleConstants.ReturnAttachmentsScope;

    public IAuthorizationRequirement Create(File file, string permission)
    {
        return new ReturnAuthorizationRequirement { Permission = permission };
    }
}
