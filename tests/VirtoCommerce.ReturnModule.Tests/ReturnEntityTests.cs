using System;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Data.Models;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnEntityTests
{
    private static readonly DateTime _submitted = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Conversion_CarriesTheSubmitDateBothWays()
    {
        var entity = new ReturnEntity().FromModel(new Return { SubmittedDate = _submitted }, new PrimaryKeyResolvingMap());

        Assert.Equal(_submitted, entity.ToModel(new Return()).SubmittedDate);
    }

    [Fact]
    public void Patch_FirstSubmitDate_IsRecorded()
    {
        var stored = new ReturnEntity();

        new ReturnEntity { SubmittedDate = _submitted }.Patch(stored);

        Assert.Equal(_submitted, stored.SubmittedDate);
    }

    [Fact]
    public void Patch_StoredSubmitDate_IsNeitherClearedNorReplaced()
    {
        // A writer built before the field existed sends none; the organization's view must not lose the return.
        var stored = new ReturnEntity { SubmittedDate = _submitted };

        new ReturnEntity().Patch(stored);
        new ReturnEntity { SubmittedDate = _submitted.AddDays(1) }.Patch(stored);

        Assert.Equal(_submitted, stored.SubmittedDate);
    }
}
