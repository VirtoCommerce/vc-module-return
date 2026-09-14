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

    public IList<CreateReturnItemRequest> Items { get; set; }

    /// <summary>
    /// Filled by the builder from the token, never accepted as input.
    /// </summary>
    public string CustomerId { get; set; }
}

public class CreateReturnCommandType : InputObjectGraphType<CreateReturnCommand>
{
    public CreateReturnCommandType()
    {
        Field(x => x.OrderId, nullable: false);
        Field(x => x.CustomerReference, nullable: true);
        Field(x => x.CustomerComment, nullable: true);
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<InputReturnItemType>>>>("items");
    }
}
