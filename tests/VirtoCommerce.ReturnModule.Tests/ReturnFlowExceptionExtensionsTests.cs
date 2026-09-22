using GraphQL;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.ExperienceApi.Extensions;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnFlowExceptionExtensionsTests
{
    [Fact]
    public void Values_BecomeErrorExtensions()
    {
        var exception = new ReturnFlowException(ReturnFlowError.QuantityUnavailable, "asked for 6, 4 available")
            .WithValue(ReturnFlowErrorValue.OrderLineItemId, "line-1")
            .WithValue(ReturnFlowErrorValue.RequestedQuantity, 6)
            .WithValue(ReturnFlowErrorValue.AvailableQuantity, 4);

        var error = exception.ToExecutionError();

        Assert.Equal(ReturnFlowError.QuantityUnavailable, error.Code);
        Assert.Equal("asked for 6, 4 available", error.Message);
        Assert.Equal("line-1", error.Extensions[ReturnFlowErrorValue.OrderLineItemId]);
        Assert.Equal(6, error.Extensions[ReturnFlowErrorValue.RequestedQuantity]);
        Assert.Equal(4, error.Extensions[ReturnFlowErrorValue.AvailableQuantity]);
    }

    [Fact]
    public void ErrorWithoutValues_CarriesNoExtensionsOfItsOwn()
    {
        var error = new ReturnFlowException(ReturnFlowError.WrongStatus, "cannot be submitted").ToExecutionError();

        Assert.Equal(ReturnFlowError.WrongStatus, error.Code);
        Assert.Null(error.Extensions);
    }

    /// <summary>
    /// The platform serialises extensions and not Exception.Data, so values put on the exception
    /// itself would never reach the client. This is what pins the choice of one over the other.
    /// </summary>
    [Fact]
    public void Values_AreVisibleThroughTheProviderThePlatformConfigures()
    {
        var exception = new ReturnFlowException(ReturnFlowError.QuantityUnavailable, "asked for 6, 4 available")
            .WithValue(ReturnFlowErrorValue.AvailableQuantity, 4);

        var provider = new GraphQL.Execution.ErrorInfoProvider(new GraphQL.Execution.ErrorInfoProviderOptions
        {
            ExposeExtensions = true,
            ExposeData = false,
        });

        var info = provider.GetInfo(exception.ToExecutionError());

        Assert.Equal(4, info.Extensions[ReturnFlowErrorValue.AvailableQuantity]);
        Assert.Equal(ReturnFlowError.QuantityUnavailable, info.Extensions["code"]);
    }
}
