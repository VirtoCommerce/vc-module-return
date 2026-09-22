using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ReturnModule.Data.Services;

public class ReturnSettingsService : IReturnSettingsService
{
    private static readonly char[] _settingSeparators = [',', ';'];

    private readonly IStoreService _storeService;
    private readonly ILocalizableSettingService _localizableSettingService;

    public ReturnSettingsService(IStoreService storeService, ILocalizableSettingService localizableSettingService)
    {
        _storeService = storeService;
        _localizableSettingService = localizableSettingService;
    }

    public virtual async Task<ReturnStoreRules> GetRulesAsync(string storeId)
    {
        var store = string.IsNullOrEmpty(storeId) ? null : await _storeService.GetNoCloneAsync(storeId);
        var settings = store?.Settings ?? [];

        var result = AbstractTypeFactory<ReturnStoreRules>.TryCreateInstance();

        result.Reasons = await GetReasonsAsync();
        result.ReasonsRequiringComment = ParseSetting(settings, ModuleConstants.Settings.General.ReturnReasonsRequiringComment);
        result.AttachmentsRequired = settings.GetValue<bool>(ModuleConstants.Settings.General.ReturnAttachmentsRequired);
        result.SendNotifications = settings.GetValue<bool>(ModuleConstants.Settings.General.ReturnSendNotifications);
        result.SendPushNotifications = settings.GetValue<bool>(ModuleConstants.Settings.General.ReturnSendPushNotifications);

        return result;
    }

    // Return.Reasons is a dictionary setting: GetValue would answer the store's single current value
    // rather than the items an operator actually offers, so only one reason would ever validate.
    protected virtual async Task<IList<string>> GetReasonsAsync()
    {
        var values = await _localizableSettingService.GetValuesAsync(ModuleConstants.Settings.General.ReturnReasons.Name, null);

        return values.Select(x => x.Key).ToList();
    }

    protected static IList<string> ParseSetting(IEnumerable<ObjectSettingEntry> settings, SettingDescriptor setting)
    {
        return settings.GetValue<string>(setting)
            ?.Split(_settingSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
    }
}
