using System;
using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.ReturnModule.ExperienceApi.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnsQuery : SearchQuery<ReturnSearchResult>
{
    public string CustomerId { get; set; }

    public ReturnScope Scope { get; set; }

    /// <summary>
    /// Set by the builder once the caller is known to be allowed to see it, never taken from the request.
    /// </summary>
    public string OrganizationId { get; set; }

    public string StoreId { get; set; }

    public IList<string> Statuses { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public override IEnumerable<QueryArgument> GetArguments()
    {
        foreach (var argument in base.GetArguments())
        {
            yield return argument;
        }

        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(StoreId));
        yield return Argument<ReturnScopeType>(nameof(Scope), "OWN when omitted. ORGANIZATION is refused, not narrowed, for a caller who may not see it.");
        yield return Argument<ListGraphType<StringGraphType>>(nameof(Statuses));
        yield return Argument<DateTimeGraphType>(nameof(StartDate));
        yield return Argument<DateTimeGraphType>(nameof(EndDate), "Inclusive of the given instant, so send the end of the day.");
    }

    public override void Map(IResolveFieldContext context)
    {
        base.Map(context);

        StoreId = context.GetArgument<string>(nameof(StoreId));
        Scope = context.GetArgument(nameof(Scope), ReturnScope.Own);
        Statuses = context.GetArgument<IList<string>>(nameof(Statuses));
        StartDate = context.GetArgument<DateTime?>(nameof(StartDate));
        EndDate = context.GetArgument<DateTime?>(nameof(EndDate));
    }
}
