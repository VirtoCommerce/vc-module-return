using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnableItemsQuery : Query<IList<ReturnableItem>>
{
    public string OrderId { get; set; }

    public string CustomerId { get; set; }

    public override IEnumerable<QueryArgument> GetArguments()
    {
        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(OrderId));
    }

    public override void Map(IResolveFieldContext context)
    {
        OrderId = context.GetArgument<string>(nameof(OrderId));
    }
}
