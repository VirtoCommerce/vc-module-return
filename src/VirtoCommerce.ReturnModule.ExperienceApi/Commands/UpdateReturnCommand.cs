using System.Collections.Generic;
using GraphQL.Types;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Commands;

public class UpdateReturnCommand : ICommand<Return>
{
    public string ReturnId { get; set; }

    public string CustomerReference { get; set; }

    public string CustomerComment { get; set; }

    public IList<CreateReturnItemRequest> Items { get; set; }

    public string CustomerId { get; set; }
}

public class UpdateReturnCommandType : InputObjectGraphType<UpdateReturnCommand>
{
    public UpdateReturnCommandType()
    {
        Field(x => x.ReturnId, nullable: false);
        Field(x => x.CustomerReference, nullable: true);
        Field(x => x.CustomerComment, nullable: true);
        Field<ListGraphType<NonNullGraphType<InputReturnItemType>>>("items");
    }
}
