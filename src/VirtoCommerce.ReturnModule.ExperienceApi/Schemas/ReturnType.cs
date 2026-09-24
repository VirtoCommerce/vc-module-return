using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Schemas;

public class ReturnType : ExtendableGraphType<Return>
{
    public ReturnType()
    {
        Field(x => x.Id, nullable: false);
        Field(x => x.Number, nullable: false);
        Field(x => x.Status, nullable: true);

        Field<StringGraphType>("statusDisplayValue")
            .Description("Status as the Return.Status dictionary spells it in the requested culture.")
            .Argument<StringGraphType>("cultureName")
            .ResolveAsync(async context =>
            {
                var status = context.Source.Status;

                if (string.IsNullOrEmpty(status))
                {
                    return null;
                }

                var values = await context.RequestServices
                    .GetRequiredService<ILocalizableSettingService>()
                    .GetValuesAsync(ModuleConstants.Settings.General.OrderStatus.Name, context.GetArgument<string>("cultureName"));

                var value = values.FirstOrDefault(x => x.Key.EqualsIgnoreCase(status))?.Value;

                return string.IsNullOrEmpty(value) ? status : value;
            });

        Field(x => x.CreatedDate, nullable: false);
        Field(x => x.OrderId, nullable: true);
        Field(x => x.OrderNumber, nullable: true);
        Field(x => x.CustomerId, nullable: true).Description("User who raised the return; the only one who may change it.");
        Field(x => x.CustomerName, nullable: true).Description("Who raised the return, as the order names them.");
        Field(x => x.OrganizationId, nullable: true);
        Field(x => x.OrganizationName, nullable: true);
        Field(x => x.CustomerReference, nullable: true).Description("Buyer's own purchase order reference.");
        Field(x => x.CustomerComment, nullable: true);
        Field(x => x.RejectReason, nullable: true);
        Field(x => x.CancelReason, nullable: true).Description("Why the buyer withdrew the return.");

        Field<NonNullGraphType<IntGraphType>>("itemsQuantity")
            .Description("Total quantity requested across the return's lines.")
            .Resolve(context => context.Source.LineItems?.Sum(x => x.Quantity) ?? 0);

        Field<NonNullGraphType<IntGraphType>>("approvedQuantity")
            .Description("Total quantity authorized across the return's lines.")
            .Resolve(context => context.Source.LineItems?.Sum(x => x.ApprovedQuantity) ?? 0);

        Field<NonNullGraphType<ListGraphType<NonNullGraphType<ReturnLineItemType>>>>("items")
            .Resolve(context => (IEnumerable<ReturnLineItem>)context.Source.LineItems ?? []);

        Field<NonNullGraphType<ListGraphType<NonNullGraphType<ReturnActionType>>>>("availableActions")
            .Description("Every known action, each flagged with whether it would be accepted now.")
            .ResolveAsync(async context =>
            {
                var flowService = context.RequestServices.GetRequiredService<IReturnFlowService>();
                var actions = flowService.GetAvailableActions(context.Source);

                // A colleague reading the return through the organization scope would be refused by
                // every mutation, so offering them the buttons would only lead to an error.
                if (!await flowService.IsOwnedBy(context.Source, context.GetCurrentUserId()))
                {
                    foreach (var action in actions)
                    {
                        action.IsAvailable = false;
                        action.UnavailableReason = ReturnFlowError.ReturnNotFound;
                    }
                }

                return (IEnumerable<ReturnFlowAction>)actions;
            });
    }
}
