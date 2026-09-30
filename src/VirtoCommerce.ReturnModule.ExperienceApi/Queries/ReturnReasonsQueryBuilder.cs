using System.Collections.Generic;
using GraphQL.Types;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnReasonsQueryBuilder : ReturnQueryBuilderBase<ReturnReasonsQuery, IList<ReturnReason>, ListGraphType<ReturnReasonType>>
{
    protected override string Name => "returnReasons";

    public ReturnReasonsQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }
}
