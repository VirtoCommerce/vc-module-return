using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Services;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

// Every writer saves through ReturnService - the storefront flow, PUT /api/return, custom code - so what it
// fills in on save is what a return has, whoever wrote it.
public class ReturnServiceTests
{
    private readonly CustomerOrder _order = new()
    {
        Id = "order-1",
        Number = "CO-1",
        StoreId = "B2B-store",
        CustomerId = "user-1",
        CustomerName = "Jan de Vries",
        LanguageCode = "de-DE",
        Items = [new LineItem { Id = "order-line-1", ProductId = "product-1", Sku = "SKU-1", Name = "Drill", Quantity = 5, Price = 12.5m }],
    };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Save_ReturnWithoutALanguage_TakesTheOrdersLanguage(string languageCode)
    {
        // CreateDraft stores only the culture the caller sent, so without one this is where the return gets
        // the language its emails and push messages are written in.
        var orderReturn = NewReturn(languageCode);

        await Save(orderReturn);

        Assert.Equal("de-DE", orderReturn.LanguageCode);
    }

    [Fact]
    public async Task Save_ReturnWithALanguage_KeepsIt()
    {
        var orderReturn = NewReturn("fr-FR");

        await Save(orderReturn);

        Assert.Equal("fr-FR", orderReturn.LanguageCode);
    }

    [Fact]
    public async Task Save_MissingSnapshots_AreTakenFromTheOrder_SetOnesAreKept()
    {
        var orderReturn = NewReturn("fr-FR");
        orderReturn.CustomerName = "Buyer as typed";
        orderReturn.LineItems.Single().Name = "Drill, blue";

        await Save(orderReturn);

        Assert.Equal("B2B-store", orderReturn.StoreId);
        Assert.Equal("CO-1", orderReturn.OrderNumber);
        Assert.Equal("Buyer as typed", orderReturn.CustomerName);

        var lineItem = Assert.Single(orderReturn.LineItems);
        Assert.Equal("SKU-1", lineItem.Sku);
        Assert.Equal("Drill, blue", lineItem.Name);
        Assert.Equal(5, lineItem.OrderedQuantity);
        Assert.Equal(12.5m, lineItem.Price);
    }

    private Task Save(Return orderReturn)
    {
        var orderService = new Mock<ICustomerOrderService>();
        orderService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([_order]);

        return new TestableReturnService(orderService.Object).RunBeforeSaveChanges([orderReturn]);
    }

    // With a number set, saving generates none, so it needs no store and no settings.
    private static Return NewReturn(string languageCode)
    {
        return new Return
        {
            Number = "RET260929-00001",
            OrderId = "order-1",
            LanguageCode = languageCode,
            LineItems = [new ReturnLineItem { OrderLineItemId = "order-line-1", Quantity = 2 }],
        };
    }

    private sealed class TestableReturnService : ReturnService
    {
        public TestableReturnService(ICustomerOrderService orderService)
            : base(() => null, Mock.Of<IPlatformMemoryCache>(), Mock.Of<IEventPublisher>(), orderService, Mock.Of<IUniqueNumberGenerator>(), Mock.Of<IStoreService>(), Mock.Of<ISettingsManager>())
        {
        }

        public Task RunBeforeSaveChanges(IList<Return> returns) => BeforeSaveChanges(returns);
    }
}
