using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Web.Controllers.Api;
using Xunit;
using Permissions = VirtoCommerce.ReturnModule.Core.ModuleConstants.Security.Permissions;

namespace VirtoCommerce.ReturnModule.Tests;

/// <summary>
/// The controller tests call the actions directly, so MVC filters never run. With no fallback policy
/// on the platform and no attribute on the class, an action that lost its attribute would be open to
/// anyone - this pins each one.
/// </summary>
public class ReturnPermissionsTests
{
    [Theory]
    [InlineData(nameof(ReturnController.SearchReturns), Permissions.Read)]
    [InlineData(nameof(ReturnController.GetReturnById), Permissions.Read)]
    [InlineData(nameof(ReturnController.GetAvailableStatuses), Permissions.Read)]
    [InlineData(nameof(ReturnController.UpdateReturn), Permissions.Update)]
    [InlineData(nameof(ReturnController.AuthorizeReturn), Permissions.Authorize)]
    [InlineData(nameof(ReturnController.DeleteReturn), Permissions.Delete)]
    [InlineData(nameof(ReturnController.GetAvailableQuantities), Permissions.Read)]
    public void EachAction_RequiresItsPermission(string action, string permission)
    {
        var method = typeof(ReturnController).GetMethod(action);

        var attribute = Assert.Single(method!.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(permission, attribute.Policy);
    }

    [Fact]
    public void EveryAction_IsCoveredAbove()
    {
        var actions = typeof(ReturnController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(x => x.Name)
            .OrderBy(x => x);

        Assert.Equal(
            new[]
            {
                nameof(ReturnController.AuthorizeReturn),
                nameof(ReturnController.DeleteReturn),
                nameof(ReturnController.GetAvailableQuantities),
                nameof(ReturnController.GetAvailableStatuses),
                nameof(ReturnController.GetReturnById),
                nameof(ReturnController.SearchReturns),
                nameof(ReturnController.UpdateReturn),
            },
            actions);
    }

    [Fact]
    public void EveryPermission_IsRegistered()
    {
        // A policy whose permission is not registered fails its lookup, turning every call into a 500.
        var constants = typeof(Permissions)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.IsLiteral)
            .Select(x => (string)x.GetRawConstantValue())
            .OrderBy(x => x);

        Assert.Equal(constants, ModuleConstants.Security.Permissions.AllPermissions.OrderBy(x => x));
    }
}
