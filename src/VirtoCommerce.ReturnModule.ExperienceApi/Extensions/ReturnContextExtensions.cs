using GraphQL;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Security.Authorization;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Extensions;

public static class ReturnContextExtensions
{
    // GetCurrentUserId answers "Anonymous" rather than nothing for an unauthenticated caller, and that
    // is a real persisted CustomerId on stores with guest checkout. An empty one is no safer: the
    // search service drops the owner filter entirely and returns the whole store.
    public static string GetOwnCustomerId(this IResolveFieldContext context)
    {
        if (!context.IsAuthenticated())
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }

        var customerId = context.GetCurrentUserId();

        if (string.IsNullOrEmpty(customerId) || customerId.EqualsIgnoreCase(ModuleConstants.AnonymousUser.UserName))
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }

        return customerId;
    }
}
