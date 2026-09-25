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

    public string LanguageCode { get; set; }

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
        Field(x => x.LanguageCode, nullable: true);
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<InputReturnItemType>>>>("items");
    }
}
