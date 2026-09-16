using System.Collections.Generic;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

public interface IReturnStateProvider
{
    IList<string> Actions { get; }

    bool IsAllowed(string action, string status);

    string GetNextStatus(string action, string status);

    IList<ReturnFlowAction> GetActions(Return orderReturn);
}
