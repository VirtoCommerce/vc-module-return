using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnsQuery : SearchQuery<ReturnSearchResult>
{
    /// <summary>
    /// Taken from the token by the query builder, never from an argument — a buyer must not be
    /// able to ask for somebody else's returns.
    /// </summary>
    public string CustomerId { get; set; }

    public IList<string> Statuses { get; set; }

    public override IEnumerable<QueryArgument> GetArguments()
    {
        foreach (var argument in base.GetArguments())
        {
            yield return argument;
        }

        yield return Argument<ListGraphType<StringGraphType>>(nameof(Statuses));
    }

    public override void Map(IResolveFieldContext context)
    {
        base.Map(context);

        Statuses = context.GetArgument<IList<string>>(nameof(Statuses));
    }
}
