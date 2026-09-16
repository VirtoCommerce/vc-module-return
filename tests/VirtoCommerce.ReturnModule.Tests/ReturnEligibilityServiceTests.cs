using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnEligibilityServiceTests
{
    private const string StoreId = "store-1";
    private const string LineId = "line-1";

    [Fact]
    public async Task ReturnsDisabled_NothingIsReturnable_AndTheReasonSaysSo()
    {
        var service = CreateService(enabled: false);
        var order = CreateOrder(deliveredDaysAgo: 1);

        var eligibility = await service.GetOrderEligibility(order);
        var items = await service.GetReturnableItems(order);

        Assert.False(eligibility.IsEligible);
        Assert.Equal(ReturnIneligibilityReason.ReturnsDisabled, eligibility.Reason);

        Assert.Equal(ReturnIneligibilityReason.ReturnsDisabled, items.Single().IneligibilityReason);
    }

    [Fact]
    public async Task OrderStatusNotAllowed_LineIsNotReturnable()
    {
        var service = CreateService();
        var order = CreateOrder(deliveredDaysAgo: 1, status: "Processing");

        var items = await service.GetReturnableItems(order);

        Assert.False(items.Single().IsReturnable);
        Assert.Equal(ReturnIneligibilityReason.OrderStatusNotAllowed, items.Single().IneligibilityReason);
    }

    [Fact]
    public async Task NoDeliveryDate_LineIsNotReturnable()
    {
        var service = CreateService();
        var order = CreateOrder(deliveredDaysAgo: null);

        var items = await service.GetReturnableItems(order);

        Assert.Equal(ReturnIneligibilityReason.NotDelivered, items.Single().IneligibilityReason);
        Assert.Null(items.Single().DeliveryDate);
    }

    [Fact]
    public async Task DeliveredLongerAgoThanTheWindow_LineIsOutsideIt()
    {
        var service = CreateService(windowDays: 30);
        var order = CreateOrder(deliveredDaysAgo: 31);

        var items = await service.GetReturnableItems(order);

        Assert.Equal(ReturnIneligibilityReason.OutsideReturnWindow, items.Single().IneligibilityReason);
    }

    [Fact]
    public async Task SeveralDeliveries_WindowRunsFromTheLatest()
    {
        var service = CreateService(windowDays: 30);

        var order = CreateOrder(deliveredDaysAgo: 40);
        order.Shipments.Add(CreateShipment(daysAgo: 2, quantity: 3));

        var items = await service.GetReturnableItems(order);
        var item = items.Single();

        Assert.True(item.IsReturnable);
        Assert.Equal(DateTime.UtcNow.AddDays(-2).Date, item.DeliveryDate?.Date);
        Assert.Equal(8, item.DeliveredQuantity);
    }

    [Fact]
    public async Task CancelledShipment_DoesNotCountAsDelivered()
    {
        var service = CreateService();

        var order = CreateOrder(deliveredDaysAgo: 1);
        order.Shipments.Single().IsCancelled = true;

        var items = await service.GetReturnableItems(order);

        Assert.Equal(ReturnIneligibilityReason.NotDelivered, items.Single().IneligibilityReason);
    }

    [Fact]
    public async Task CancelledLine_IsNotReturnable()
    {
        var service = CreateService();

        var order = CreateOrder(deliveredDaysAgo: 1);
        order.Items.Single().IsCancelled = true;

        var items = await service.GetReturnableItems(order);

        Assert.Equal(ReturnIneligibilityReason.LineCancelled, items.Single().IneligibilityReason);
    }

    [Fact]
    public async Task ReturnableQuantity_IsCappedByWhatWasDelivered()
    {
        var service = CreateService();
        var order = CreateOrder(deliveredDaysAgo: 1, orderedQuantity: 10, deliveredQuantity: 4);

        var items = await service.GetReturnableItems(order);

        Assert.Equal(10, items.Single().OrderedQuantity);
        Assert.Equal(4, items.Single().DeliveredQuantity);
        Assert.Equal(4, items.Single().ReturnableQuantity);
    }

    [Fact]
    public async Task HeldQuantity_ComesOffWhatIsReturnable()
    {
        var service = CreateService(heldQuantity: 3);
        var order = CreateOrder(deliveredDaysAgo: 1, orderedQuantity: 5, deliveredQuantity: 5);

        var items = await service.GetReturnableItems(order);

        Assert.Equal(2, items.Single().ReturnableQuantity);
    }

    [Fact]
    public async Task EverythingAlreadyRequested_LeavesNothingToReturn()
    {
        var service = CreateService(heldQuantity: 5);
        var order = CreateOrder(deliveredDaysAgo: 1, orderedQuantity: 5, deliveredQuantity: 5);

        var items = await service.GetReturnableItems(order);

        Assert.Equal(0, items.Single().ReturnableQuantity);
        Assert.Equal(ReturnIneligibilityReason.NothingLeftToReturn, items.Single().IneligibilityReason);
    }

    private static ReturnEligibilityService CreateService(
        bool enabled = true,
        int windowDays = 30,
        int heldQuantity = 0)
    {
        var store = new Store
        {
            Id = StoreId,
            Settings =
            [
                Setting(ModuleConstants.Settings.General.ReturnEnabled.Name, enabled),
                Setting(ModuleConstants.Settings.General.ReturnWindowDays.Name, windowDays),
                Setting(ModuleConstants.Settings.General.ReturnAllowedOrderStatuses.Name, "Completed"),
                Setting(ModuleConstants.Settings.General.ReturnAllowedShipmentStatuses.Name, string.Empty),
            ],
        };

        // GetNoCloneAsync is an extension over the CRUD contract, so the mock answers what it calls.
        var storeService = new Mock<IStoreService>();
        storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((IList<string> ids, string _, bool _) =>
                ids.Contains(StoreId) ? [store] : new List<Store>());

        var quantityService = new Mock<IReturnQuantityService>();
        quantityService
            .Setup(x => x.GetHeldQuantities(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(heldQuantity > 0
                ? new Dictionary<string, int> { [LineId] = heldQuantity }
                : new Dictionary<string, int>());

        return new ReturnEligibilityService(storeService.Object, quantityService.Object);
    }

    private static ObjectSettingEntry Setting(string name, object value)
    {
        var result = new ObjectSettingEntry { Name = name };
        result.Value = value;

        return result;
    }

    private static CustomerOrder CreateOrder(
        int? deliveredDaysAgo,
        string status = "Completed",
        int orderedQuantity = 5,
        int deliveredQuantity = 5)
    {
        var order = new CustomerOrder
        {
            Id = "order-1",
            StoreId = StoreId,
            Status = status,
            Items = [new LineItem { Id = LineId, Quantity = orderedQuantity }],
            Shipments = [],
        };

        order.Shipments.Add(CreateShipment(deliveredDaysAgo, deliveredQuantity));

        return order;
    }

    private static Shipment CreateShipment(int? daysAgo, int quantity)
    {
        return new Shipment
        {
            DeliveryDate = daysAgo == null ? null : DateTime.UtcNow.AddDays(-daysAgo.Value),
            Items = [new ShipmentItem { LineItemId = LineId, Quantity = quantity }],
        };
    }
}
