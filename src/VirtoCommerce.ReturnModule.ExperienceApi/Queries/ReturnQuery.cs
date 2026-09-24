using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnQuery : Query<Return>
{
    public string Id { get; set; }

    public string CustomerId { get; set; }

    /// <summary>
    /// The organization whose returns the caller may read besides their own; empty when none.
    /// </summary>
    public string OrganizationId { get; set; }

    public override IEnumerable<QueryArgument> GetArguments()
    {
        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(Id));
    }

    public override void Map(IResolveFieldContext context)
    {
        Id = context.GetArgument<string>(nameof(Id));
    }
}
