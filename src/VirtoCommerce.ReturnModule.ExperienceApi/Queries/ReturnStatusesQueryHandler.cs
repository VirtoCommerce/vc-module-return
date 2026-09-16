using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Xapi.Core.Queries;
using ReturnSettings = VirtoCommerce.ReturnModule.Core.ModuleConstants.Settings.General;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnStatusesQueryHandler : LocalizedSettingQueryHandler<ReturnStatusesQuery>
{
    public ReturnStatusesQueryHandler(ILocalizableSettingService localizableSettingService)
        : base(localizableSettingService)
    {
    }

    protected override SettingDescriptor Setting => ReturnSettings.OrderStatus;
}
