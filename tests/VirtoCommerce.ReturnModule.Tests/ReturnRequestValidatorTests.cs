using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Data.Validation;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnRequestValidatorTests
{
    [Fact]
    public async Task ReasonOutsideTheDictionary_IsRefused()
    {
        var result = await Validate(new CreateReturnItemRequest { ReasonCode = "totally-made-up" });

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ReasonFromTheDictionary_IsAccepted()
    {
        var result = await Validate(new CreateReturnItemRequest { ReasonCode = "NoLongerNeeded" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ReasonIsMatchedIgnoringCase()
    {
        var result = await Validate(new CreateReturnItemRequest { ReasonCode = "nolongerneeded" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ReasonNeedingAComment_WithoutOne_IsRefused()
    {
        var result = await Validate(new CreateReturnItemRequest { ReasonCode = "FaultyOnArrival" });

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ReasonNeedingAComment_WithOne_IsAccepted()
    {
        var result = await Validate(new CreateReturnItemRequest
        {
            ReasonCode = "FaultyOnArrival",
            ReasonComment = "Arrived cracked",
        });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(65, 0, 0)]
    [InlineData(0, 1025, 0)]
    [InlineData(0, 0, 129)]
    public async Task OverLongStrings_AreRefusedRatherThanReachingTheDatabase(int reason, int comment, int serial)
    {
        var result = await Validate(new CreateReturnItemRequest
        {
            ReasonCode = reason > 0 ? new string('x', reason) : "NoLongerNeeded",
            ReasonComment = comment > 0 ? new string('x', comment) : null,
            SerialNumber = serial > 0 ? new string('x', serial) : null,
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task OverLongCustomerReference_IsRefused()
    {
        var context = CreateContext(new CreateReturnItemRequest { ReasonCode = "NoLongerNeeded" });
        context.CustomerReference = new string('x', 129);

        var result = await new ReturnRequestValidator().ValidateAsync(context, TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
    }

    private static Task<FluentValidation.Results.ValidationResult> Validate(CreateReturnItemRequest item)
    {
        return new ReturnRequestValidator().ValidateAsync(CreateContext(item), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task EmptyReason_IsAcceptedWhileDrafting()
    {
        var result = await Validate(new CreateReturnItemRequest { ReasonCode = "" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task EmptyReason_IsRefusedWhenOneIsRequired()
    {
        var context = CreateContext(new CreateReturnItemRequest { ReasonCode = "" });
        context.RequireReason = true;

        var result = await new ReturnRequestValidator().ValidateAsync(context, TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
    }

    private static ReturnRequestValidationContext CreateContext(CreateReturnItemRequest item)
    {
        return new ReturnRequestValidationContext
        {
            Items = [item],
            Reasons = ["FaultyOnArrival", "NoLongerNeeded"],
            ReasonsRequiringComment = ["FaultyOnArrival"],
        };
    }
}
