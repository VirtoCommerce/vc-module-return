using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Extensions;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.ExperienceApi.Models;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnPolicyQueryHandler : IQueryHandler<ReturnPolicyQuery, ReturnPolicy>
{
    private static readonly char[] _separators = [',', ';'];

    private readonly IStoreService _storeService;

    public ReturnPolicyQueryHandler(IStoreService storeService)
    {
        _storeService = storeService;
    }

    public virtual async Task<ReturnPolicy> Handle(ReturnPolicyQuery request, CancellationToken cancellationToken)
    {
        var store = await _storeService.GetNoCloneAsync(request.StoreId);

        var settings = store?.Settings ?? Array.Empty<ObjectSettingEntry>() as IEnumerable<ObjectSettingEntry>;

        var result = AbstractTypeFactory<ReturnPolicy>.TryCreateInstance();
        result.IsEnabled = settings.GetValue<bool>(ModuleConstants.Settings.General.ReturnEnabled);
        result.WindowDays = settings.GetValue<int>(ModuleConstants.Settings.General.ReturnWindowDays);
        result.AllowedOrderStatuses = settings
            .GetValue<string>(ModuleConstants.Settings.General.ReturnAllowedOrderStatuses)
            ?.Split(_separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

        return result;
    }
}
