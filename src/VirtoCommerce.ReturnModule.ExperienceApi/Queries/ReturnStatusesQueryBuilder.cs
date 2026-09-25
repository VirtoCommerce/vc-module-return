using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.Queries;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnStatusesQueryBuilder : LocalizedSettingQueryBuilder<ReturnStatusesQuery>
{
    protected override string Name => "returnStatuses";

    public ReturnStatusesQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }
}
