using System.Collections.Generic;
using GraphQL;
using VirtoCommerce.ReturnModule.Core;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Extensions;

public static class ReturnFlowExceptionExtensions
{
    /// <summary>
    /// Carries the error's values into the GraphQL response as extensions, next to the code, so a
    /// client can render "you asked for 6, only 4 are left" in its own language instead of parsing
    /// the English message. ExposeExtensions is on in the platform's GraphQL setup, which is what
    /// makes these visible; ExposeData is not, so Exception.Data would go nowhere.
    /// </summary>
    public static ExecutionError ToExecutionError(this ReturnFlowException exception)
    {
        var result = new ExecutionError(exception.Message, exception) { Code = exception.Code };

        if (exception.Values.Count == 0)
        {
            return result;
        }

        result.Extensions ??= new Dictionary<string, object>();

        foreach (var value in exception.Values)
        {
            result.Extensions[value.Key] = value.Value;
        }

        return result;
    }
}
