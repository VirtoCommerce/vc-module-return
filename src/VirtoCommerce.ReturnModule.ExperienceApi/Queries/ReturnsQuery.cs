using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnsQuery : SearchQuery<ReturnSearchResult>
{
    public string CustomerId { get; set; }

    public string StoreId { get; set; }

    public IList<string> Statuses { get; set; }

    public override IEnumerable<QueryArgument> GetArguments()
    {
        foreach (var argument in base.GetArguments())
        {
            yield return argument;
        }

        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(StoreId));
        yield return Argument<ListGraphType<StringGraphType>>(nameof(Statuses));
    }

    public override void Map(IResolveFieldContext context)
    {
        base.Map(context);

        StoreId = context.GetArgument<string>(nameof(StoreId));
        Statuses = context.GetArgument<IList<string>>(nameof(Statuses));
    }
}
