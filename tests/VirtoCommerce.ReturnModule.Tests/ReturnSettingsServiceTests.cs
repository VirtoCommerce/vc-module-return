using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnSettingsServiceTests
{
    [Fact]
    public async Task Reasons_ComeFromTheDictionaryItems_NotTheSettingValue()
    {
        // Return.Reasons is a dictionary setting: its value holds the store's single current pick,
        // while the items an operator actually offers live in the localization store. Reading the
        // value would let exactly one reason validate.
        var service = CreateService(
            reasons: ["FaultyOnArrival", "DamagedInTransit", "NoLongerNeeded"],
            settings: [Setting("Return.Reasons", SettingValueType.ShortText, "FaultyOnArrival")]);

        var rules = await service.GetRulesAsync("store-1");

        Assert.Equal(["FaultyOnArrival", "DamagedInTransit", "NoLongerNeeded"], rules.Reasons);
    }

    [Theory]
    [InlineData("FaultyOnArrival,DamagedInTransit")]
    [InlineData("FaultyOnArrival;DamagedInTransit")]
    [InlineData(" FaultyOnArrival , DamagedInTransit ")]
    public async Task ReasonsRequiringComment_AreSplitAndTrimmed(string value)
    {
        var service = CreateService(
            settings: [Setting("Return.ReasonsRequiringComment", SettingValueType.ShortText, value)]);

        var rules = await service.GetRulesAsync("store-1");

        Assert.Equal(["FaultyOnArrival", "DamagedInTransit"], rules.ReasonsRequiringComment);
    }

    [Fact]
    public async Task AttachmentsRequired_IsReadFromTheStore()
    {
        var service = CreateService(
            settings: [Setting("Return.AttachmentsRequired", SettingValueType.Boolean, true)]);

        var rules = await service.GetRulesAsync("store-1");

        Assert.True(rules.AttachmentsRequired);
    }

    [Fact]
    public async Task NoStoreId_FallsBackToTheShippedDefaults()
    {
        // An unconfigured store still demands a comment for the three reasons the module ships with,
        // because the setting descriptor carries them.
        var service = CreateService(reasons: ["NoLongerNeeded"]);

        var rules = await service.GetRulesAsync(null);

        Assert.Equal(["NoLongerNeeded"], rules.Reasons);
        Assert.Equal(["FaultyOnArrival", "DamagedInTransit", "WrongItemDelivered"], rules.ReasonsRequiringComment);
        Assert.False(rules.AttachmentsRequired);

        // Default on: upgrading the module starts telling buyers, with no operator opt-in.
        Assert.True(rules.SendNotifications);
        Assert.True(rules.SendPushNotifications);
    }

    [Fact]
    public void NotificationSettings_AreStoreLevel()
    {
        var storeLevelSettings = ModuleConstants.Settings.StoreLevelSettings.ToList();

        Assert.Contains(ModuleConstants.Settings.General.ReturnSendNotifications, storeLevelSettings);
        Assert.Contains(ModuleConstants.Settings.General.ReturnSendPushNotifications, storeLevelSettings);
    }

    private static ObjectSettingEntry Setting(string name, SettingValueType valueType, object value)
    {
        return new ObjectSettingEntry { Name = name, ValueType = valueType, Value = value };
    }

    private static ReturnSettingsService CreateService(
        IList<string> reasons = null,
        IList<ObjectSettingEntry> settings = null)
    {
        var storeService = new Mock<IStoreService>();
        storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([new Store { Id = "store-1", Settings = settings ?? [] }]);

        var localizableSettingService = new Mock<ILocalizableSettingService>();
        localizableSettingService
            .Setup(x => x.GetValuesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((reasons ?? []).Select(x => new KeyValue { Key = x, Value = x }).ToList());

        return new ReturnSettingsService(storeService.Object, localizableSettingService.Object);
    }
}
