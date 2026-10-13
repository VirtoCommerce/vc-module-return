using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class OrganizationReturnsQuery : ReturnsQuery
{
    public string OrganizationId { get; set; }

    public override IEnumerable<QueryArgument> GetArguments()
    {
        foreach (var argument in base.GetArguments())
        {
            yield return argument;
        }

        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(OrganizationId), "Any organization the caller belongs to. Refused, not narrowed, when the caller may not read its returns.");
    }

    public override void Map(IResolveFieldContext context)
    {
        base.Map(context);

        OrganizationId = context.GetArgument<string>(nameof(OrganizationId));
    }
}
