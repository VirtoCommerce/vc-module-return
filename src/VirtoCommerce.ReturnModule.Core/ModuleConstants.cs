using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ReturnModule.Core
{
    public static class ModuleConstants
    {
        public const string ReturnAttachmentsScope = "return-attachments";

        public static class Security
        {
            public static class Permissions
            {
                public const string Access = "return:access";
                public const string Create = "return:create";
                public const string Read = "return:read";
                public const string Update = "return:update";
                public const string Delete = "return:delete";

                public static string[] AllPermissions { get; } = { Read, Create, Access, Update, Delete };
            }
        }

        public static class Settings
        {
            public static class General
            {
                private const int DefaultWindowDays = 30;

                public static SettingDescriptor OrderStatus { get; } = new SettingDescriptor
                {
                    Name = "Return.Status",
                    ValueType = SettingValueType.ShortText,
                    GroupName = "Return|Return",
                    IsDictionary = true,
                    IsLocalizable = true,
                    DefaultValue = "New",
                    // The first five shipped with the module. Of the rest, the buyer flow writes Draft,
                    // Requested and Cancelled; PartiallyApproved and Rejected are agent-side and land
                    // with step 2. Cancelled is this module's spelling, Canceled the shipped one, and
                    // both stay so that rows carrying either keep resolving. AwaitingDelivery and
                    // Received exist as constants but are not reachable yet, so they are absent here
                    // and from the locale files.
                    AllowedValues = new[]
                    {
                        "New", "Approved", "Completed", "Canceled", "Processing",
                        ReturnStatus.Draft, ReturnStatus.Requested, ReturnStatus.PartiallyApproved,
                        ReturnStatus.Rejected, ReturnStatus.Cancelled,
                    }
                };

                public static SettingDescriptor ReturnEnabled { get; } = new SettingDescriptor
                {
                    Name = "Return.ReturnEnabled",
                    GroupName = "Return|Return",
                    ValueType = SettingValueType.Boolean,
                    DefaultValue = false,
                    IsPublic = true
                };

                public static SettingDescriptor ReturnPassword { get; } = new SettingDescriptor
                {
                    Name = "Return.ReturnPassword",
                    GroupName = "Return|Advanced",
                    ValueType = SettingValueType.SecureString,
                    DefaultValue = "qwerty"
                };

                public static SettingDescriptor ReturnNewNumberTemplate { get; } = new SettingDescriptor
                {
                    Name = "Return.ReturnNewNumberTemplate",
                    ValueType = SettingValueType.ShortText,
                    GroupName = "Return|Return",
                    DefaultValue = "RET{0:yyMMdd}-{1:D5}"
                };

                public static SettingDescriptor ReturnWindowDays { get; } = new SettingDescriptor
                {
                    Name = "Return.WindowDays",
                    ValueType = SettingValueType.PositiveInteger,
                    GroupName = "Return|Return",
                    DefaultValue = DefaultWindowDays
                };

                public static SettingDescriptor ReturnAllowedOrderStatuses { get; } = new SettingDescriptor
                {
                    Name = "Return.AllowedOrderStatuses",
                    ValueType = SettingValueType.ShortText,
                    GroupName = "Return|Return",
                    DefaultValue = "Completed"
                };

                // Blank means any status, unlike the order statuses above where blank allows nothing:
                // the Shipment.Status dictionary has no "delivered" value. Delivery is DeliveryDate.
                public static SettingDescriptor ReturnAllowedShipmentStatuses { get; } = new SettingDescriptor
                {
                    Name = "Return.AllowedShipmentStatuses",
                    ValueType = SettingValueType.ShortText,
                    GroupName = "Return|Return",
                    DefaultValue = string.Empty
                };

                public static SettingDescriptor ReturnReasons { get; } = new SettingDescriptor
                {
                    Name = "Return.Reasons",
                    ValueType = SettingValueType.ShortText,
                    GroupName = "Return|Return",
                    IsDictionary = true,
                    IsLocalizable = true,
                    DefaultValue = "FaultyOnArrival",
                    AllowedValues = new[] { "FaultyOnArrival", "DamagedInTransit", "WrongItemDelivered", "OrderedByMistake", "NoLongerNeeded" }
                };

                public static SettingDescriptor ReturnReasonsRequiringComment { get; } = new SettingDescriptor
                {
                    Name = "Return.ReasonsRequiringComment",
                    ValueType = SettingValueType.ShortText,
                    GroupName = "Return|Return",
                    DefaultValue = "FaultyOnArrival,DamagedInTransit,WrongItemDelivered"
                };

                // Off by default: uploads need a FileUpload scope in the platform configuration, and
                // until it is added every submit would fail with ATTACHMENTS_REQUIRED that the buyer
                // cannot satisfy.
                public static SettingDescriptor ReturnAttachmentsRequired { get; } = new SettingDescriptor
                {
                    Name = "Return.AttachmentsRequired",
                    ValueType = SettingValueType.Boolean,
                    GroupName = "Return|Return",
                    DefaultValue = false,
                    IsPublic = true
                };

                // Off would mean a store that upgrades the module silently stops telling buyers
                // anything, which is the opposite of what this module is for.
                public static SettingDescriptor ReturnSendNotifications { get; } = new SettingDescriptor
                {
                    Name = "Return.SendNotifications",
                    ValueType = SettingValueType.Boolean,
                    GroupName = "Return|Notifications",
                    DefaultValue = true
                };

                // Transactional pushes accumulate in the Push Messages admin list, which was agreed
                // to be acceptable, so this follows the emails rather than waiting to be switched on.
                public static SettingDescriptor ReturnSendPushNotifications { get; } = new SettingDescriptor
                {
                    Name = "Return.SendPushNotifications",
                    ValueType = SettingValueType.Boolean,
                    GroupName = "Return|Notifications",
                    DefaultValue = true
                };

                public static IEnumerable<SettingDescriptor> AllSettings
                {
                    get
                    {
                        yield return ReturnEnabled;
                        yield return ReturnPassword;
                        yield return ReturnNewNumberTemplate;
                        yield return ReturnWindowDays;
                        yield return ReturnAllowedOrderStatuses;
                        yield return ReturnAllowedShipmentStatuses;
                        yield return ReturnReasons;
                        yield return ReturnReasonsRequiringComment;
                        yield return ReturnAttachmentsRequired;
                        yield return ReturnSendNotifications;
                        yield return ReturnSendPushNotifications;
                        yield return OrderStatus;
                    }
                }
            }

            public static IEnumerable<SettingDescriptor> AllSettings
            {
                get
                {
                    return General.AllSettings;
                }
            }

            public static IEnumerable<SettingDescriptor> StoreLevelSettings
            {
                get
                {
                    yield return General.ReturnNewNumberTemplate;
                    yield return General.ReturnEnabled;
                    yield return General.ReturnWindowDays;
                    yield return General.ReturnAllowedOrderStatuses;
                    yield return General.ReturnAllowedShipmentStatuses;
                    yield return General.ReturnReasons;
                    yield return General.ReturnReasonsRequiringComment;
                    yield return General.ReturnAttachmentsRequired;
                    yield return General.ReturnSendNotifications;
                    yield return General.ReturnSendPushNotifications;
                }
            }
        }
    }
}
