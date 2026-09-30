using System.Collections.Generic;
using GraphQL.Types;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Commands;

public class CreateReturnCommand : ICommand<Return>
{
    public string OrderId { get; set; }

    public string CustomerReference { get; set; }

    public string CustomerComment { get; set; }

    public string CultureName { get; set; }

    public IList<CreateReturnItemRequest> Items { get; set; }

    public string CustomerId { get; set; }
}

public class CreateReturnCommandType : InputObjectGraphType<CreateReturnCommand>
{
    public CreateReturnCommandType()
    {
        Field(x => x.OrderId, nullable: false);
        Field(x => x.CustomerReference, nullable: true);
        Field(x => x.CustomerComment, nullable: true);
        Field(x => x.CultureName, nullable: true).Description("Culture the buyer is using, such as en-US. The return's status emails are written in it; without it, in the order's.");
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<InputReturnItemType>>>>("items");
    }
}
