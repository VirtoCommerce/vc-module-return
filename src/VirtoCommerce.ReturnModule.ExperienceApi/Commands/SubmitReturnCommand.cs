using GraphQL.Types;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Commands;

public class SubmitReturnCommand : ICommand<Return>
{
    public string ReturnId { get; set; }

    /// <summary>
    /// Filled by the builder from the token, never accepted as input.
    /// </summary>
    public string CustomerId { get; set; }
}

public class SubmitReturnCommandType : InputObjectGraphType<SubmitReturnCommand>
{
    public SubmitReturnCommandType()
    {
        Field(x => x.ReturnId, nullable: false);
    }
}
