using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ReturnModule.ExperienceApi;
using VirtoCommerce.ReturnModule.ExperienceApi.Authorization;
using VirtoCommerce.ReturnModule.ExperienceApi.Commands;
using VirtoCommerce.ReturnModule.ExperienceApi.Queries;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnSchemaBuilderTests
{
    private const string CallerId = "buyer-2";

    private readonly Mock<IReturnAccessService> _accessService = new();

    // The platform maps its user id claim types at startup; the builders read the caller's id through them.
    static ReturnSchemaBuilderTests()
    {
        ClaimsPrincipalExtensions.UserIdClaimTypes = [ClaimTypes.NameIdentifier];
    }

    [Fact]
    public void EveryBuilder_ChecksTheAccountFirst()
    {
        // A check in a base class protects only what inherits it, and the next builder is the one that
        // forgets. returnStatuses is a settings dictionary served through X-API's own base.
        var ungated = typeof(AssemblyMarker).Assembly.GetTypes()
            .Where(x => !x.IsAbstract && typeof(ISchemaBuilder).IsAssignableFrom(x))
            .Where(x => x != typeof(ReturnStatusesQueryBuilder))
            .Where(x => !IsGated(x))
            .Select(x => x.Name)
            .ToList();

        Assert.Empty(ungated);
    }

    [Fact]
    public async Task UnusableAccount_IsRefusedBeforeTheBuilderRuns()
    {
        // A locked account's token is still valid for up to 30 minutes; the builder must not get as far as
        // stamping the caller onto the command.
        _accessService
            .Setup(x => x.CheckUserStateAsync(It.IsAny<IResolveFieldContext>()))
            .ThrowsAsync(AuthorizationError.UserLocked());

        var command = new SubmitReturnCommand { ReturnId = "return-1" };

        var error = await Assert.ThrowsAsync<AuthorizationError>(() => new TestSubmitReturnCommandBuilder().Run(CreateContext(), command));

        Assert.Equal(AuthorizationError.UserLocked().Code, error.Code);
        Assert.Null(command.CustomerId);
    }

    [Fact]
    public async Task OrganizationReturns_OrganizationTheCallerMayNotRead_IsRefused()
    {
        // Refused rather than narrowed to the caller's own returns, so a client cannot mistake one list
        // for the other.
        var query = new OrganizationReturnsQuery { StoreId = "B2B-store", OrganizationId = "org-1" };

        var error = await Assert.ThrowsAsync<AuthorizationError>(() => new TestOrganizationReturnsQueryBuilder().Run(CreateContext(), query));

        Assert.Equal(AuthorizationError.Forbidden().Code, error.Code);
    }

    [Fact]
    public async Task OrganizationReturns_OrganizationTheCallerMayRead_IsSearched()
    {
        // The rule is asked about the caller from the token and the organization the request names.
        _accessService
            .Setup(x => x.CanViewOrganizationAsync(CallerId, "org-1"))
            .ReturnsAsync(true);

        var query = new OrganizationReturnsQuery { StoreId = "B2B-store", OrganizationId = "org-1" };

        await new TestOrganizationReturnsQueryBuilder().Run(CreateContext(), query);

        _accessService.Verify(x => x.CheckUserStateAsync(It.IsAny<IResolveFieldContext>()), Times.Once);
        _accessService.Verify(x => x.CanViewOrganizationAsync(CallerId, "org-1"), Times.Once);
    }

    private static bool IsGated(Type type)
    {
        Type[] gatedBases = [typeof(ReturnQueryBuilderBase<,,>), typeof(ReturnSearchQueryBuilderBase<,,,>), typeof(ReturnCommandBuilderBase<,,,>)];

        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (current.IsGenericType && gatedBases.Contains(current.GetGenericTypeDefinition()))
            {
                return true;
            }
        }

        return false;
    }

    private IResolveFieldContext<object> CreateContext()
    {
        var services = new ServiceCollection()
            .AddSingleton(_accessService.Object)
            .BuildServiceProvider();

        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, CallerId)], "test"));

        var context = new Mock<IResolveFieldContext<object>>();
        context.SetupGet(x => x.RequestServices).Returns(services);
        context.SetupGet(x => x.UserContext).Returns(new GraphQLUserContext(principal));

        return context.Object;
    }

    private sealed class TestSubmitReturnCommandBuilder : SubmitReturnCommandBuilder
    {
        public TestSubmitReturnCommandBuilder()
            : base(Mock.Of<IAuthorizationService>())
        {
        }

        public Task Run(IResolveFieldContext<object> context, SubmitReturnCommand command) => BeforeMediatorSend(context, command);
    }

    private sealed class TestOrganizationReturnsQueryBuilder : OrganizationReturnsQueryBuilder
    {
        public TestOrganizationReturnsQueryBuilder()
            : base(Mock.Of<IAuthorizationService>())
        {
        }

        public Task Run(IResolveFieldContext<object> context, OrganizationReturnsQuery query) => BeforeMediatorSend(context, query);
    }
}
