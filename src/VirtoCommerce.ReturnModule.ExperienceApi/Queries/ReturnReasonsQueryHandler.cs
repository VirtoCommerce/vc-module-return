using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnReasonsQueryHandler : IQueryHandler<ReturnReasonsQuery, IList<ReturnReason>>
{
    private static readonly char[] _separators = [',', ';'];

    private readonly IStoreService _storeService;
    private readonly ILocalizableSettingService _localizableSettingService;

    public ReturnReasonsQueryHandler(IStoreService storeService, ILocalizableSettingService localizableSettingService)
    {
        _storeService = storeService;
        _localizableSettingService = localizableSettingService;
    }

    public virtual async Task<IList<ReturnReason>> Handle(ReturnReasonsQuery request, CancellationToken cancellationToken)
    {
        var settingName = ModuleConstants.Settings.General.ReturnReasons.Name;
        var values = await _localizableSettingService.GetValuesAsync(settingName, request.CultureName);

        var requiringComment = await GetCodesRequiringCommentAsync(request.StoreId);

        return values
            .Select(x =>
            {
                var reason = AbstractTypeFactory<ReturnReason>.TryCreateInstance();
                reason.Code = x.Key;
                // A store can add a value without translating it; showing the raw code beats a blank.
                reason.LocalizedName = string.IsNullOrEmpty(x.Value) ? x.Key : x.Value;
                reason.RequiresComment = requiringComment.Contains(x.Key, StringComparer.OrdinalIgnoreCase);

                return reason;
            })
            .ToList();
    }

    protected virtual async Task<IList<string>> GetCodesRequiringCommentAsync(string storeId)
    {
        var store = string.IsNullOrEmpty(storeId) ? null : await _storeService.GetNoCloneAsync(storeId);
        var settings = store?.Settings ?? [];

        return settings.GetValue<string>(ModuleConstants.Settings.General.ReturnReasonsRequiringComment)
            ?.Split(_separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
    }
}
