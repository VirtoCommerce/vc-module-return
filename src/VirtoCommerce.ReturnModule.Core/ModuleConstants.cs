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
                    // The first four shipped with the module; the rest are the statuses the buyer
                    // flow writes.
                    AllowedValues = new[]
                    {
                        "New", "Approved", "Completed", "Processing",
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

                public static SettingDescriptor ReturnAttachmentsRequired { get; } = new SettingDescriptor
                {
                    Name = "Return.AttachmentsRequired",
                    ValueType = SettingValueType.Boolean,
                    GroupName = "Return|Return",
                    DefaultValue = true,
                    IsPublic = true
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
                }
            }
        }
    }
}
