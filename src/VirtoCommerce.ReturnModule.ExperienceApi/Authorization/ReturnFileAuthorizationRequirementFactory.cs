using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using VirtoCommerce.FileExperienceApi.Core.Authorization;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.ReturnModule.Core;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Authorization;

public class ReturnFileAuthorizationRequirementFactory : IFileAuthorizationRequirementFactory
{
    public string Scope => ModuleConstants.ReturnAttachmentsScope;

    public IAuthorizationRequirement Create(File file, string permission)
    {
        return new DenyAnonymousAuthorizationRequirement();
    }
}
