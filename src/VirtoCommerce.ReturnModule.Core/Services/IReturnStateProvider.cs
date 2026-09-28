using System.Collections.Generic;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

public interface IReturnStateProvider
{
    bool IsAllowed(string action, string status);

    string GetNextStatus(string action, string status);

    IList<ReturnFlowAction> GetActions(Return orderReturn);

    // Whether an edit may set the status. orderReturn is the return as stored, or null for one being created.
    bool CanSetStatus(Return orderReturn, string newStatus);

    bool IsDecided(ReturnLineItem lineItem);
}
