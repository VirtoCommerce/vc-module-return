using GraphQL.Types;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Commands;

public class CancelReturnCommand : ICommand<Return>
{
    public string ReturnId { get; set; }

    public string Reason { get; set; }

    /// <summary>
    /// Filled by the builder from the token, never accepted as input.
    /// </summary>
    public string CustomerId { get; set; }
}

public class CancelReturnCommandType : InputObjectGraphType<CancelReturnCommand>
{
    public CancelReturnCommandType()
    {
        Field(x => x.ReturnId, nullable: false);
        Field(x => x.Reason, nullable: true).Description("Why the buyer withdrew the return.");
    }
}
