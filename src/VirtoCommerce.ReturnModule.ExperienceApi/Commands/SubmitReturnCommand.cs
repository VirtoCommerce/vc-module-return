using GraphQL.Types;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Commands;

public class SubmitReturnCommand : ICommand<Return>
{
    public string ReturnId { get; set; }

    public string CustomerId { get; set; }
}

public class SubmitReturnCommandType : InputObjectGraphType<SubmitReturnCommand>
{
    public SubmitReturnCommandType()
    {
        Field(x => x.ReturnId, nullable: false);
    }
}
