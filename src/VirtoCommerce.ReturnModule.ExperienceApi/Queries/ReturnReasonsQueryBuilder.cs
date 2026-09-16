using System.Collections.Generic;
using GraphQL.Types;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnReasonsQueryBuilder : QueryBuilder<ReturnReasonsQuery, IList<ReturnReason>, ListGraphType<ReturnReasonType>>
{
    protected override string Name => "returnReasons";

    public ReturnReasonsQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }
}
