using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ReturnModule.Core
{
    public static class ModuleConstants
    {
        /// <summary>
        /// File storage scope the buyer's photos and documents are uploaded into. Allowed types,
        /// sizes and counts are configured on the scope itself, not here.
        /// </summary>
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
                /// <summary>
                /// A month, counted from delivery — long enough to notice a fault, short enough that
                /// a store can shorten it rather than having to lengthen it.
                /// </summary>
                private const int DefaultWindowDays = 30;

                public static SettingDescriptor OrderStatus { get; } = new SettingDescriptor
                {
                    Name = "Return.Status",
                    ValueType = SettingValueType.ShortText,
                    GroupName = "Return|Return",
                    IsDictionary = true,
                    DefaultValue = "New",
                    AllowedValues = new[] { "New", "Approved", "Completed", "Canceled", "Processing" }
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

                /// <summary>
                /// Comma-separated order statuses a return may be requested from; the platform has no
                /// multi-value settings, so the list lives in one ShortText. "Delivered" is deliberately
                /// absent: it is a shipment status, not an order one — see
                /// <see cref="ReturnAllowedShipmentStatuses"/>. Blank allows nothing; to switch returns
                /// off entirely use Return.ReturnEnabled instead.
                /// </summary>
                public static SettingDescriptor ReturnAllowedOrderStatuses { get; } = new SettingDescriptor
                {
                    Name = "Return.AllowedOrderStatuses",
                    ValueType = SettingValueType.ShortText,
                    GroupName = "Return|Return",
                    DefaultValue = "Completed"
                };

                /// <summary>
                /// Comma-separated shipment statuses that count as delivered. Blank by default, and
                /// blank means any status: the platform's Shipment.Status dictionary is
                /// New / PickPack / Cancelled / ReadyToSend / Sent, has no "delivered" value at all,
                /// and every deployment renames it, so any hardcoded default would silently block
                /// every return. With no list the filled-in delivery date is the delivery signal;
                /// set this to whatever terminal status a store actually uses to tighten it.
                /// </summary>
                public static SettingDescriptor ReturnAllowedShipmentStatuses { get; } = new SettingDescriptor
                {
                    Name = "Return.AllowedShipmentStatuses",
                    ValueType = SettingValueType.ShortText,
                    GroupName = "Return|Return",
                    DefaultValue = string.Empty
                };

                /// <summary>
                /// Reason codes a buyer picks from. Localizable, so a store can add its own without
                /// anyone shipping storefront translations for them.
                /// </summary>
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

                /// <summary>
                /// Comma-separated reason codes that oblige the buyer to explain themselves.
                /// </summary>
                public static SettingDescriptor ReturnReasonsRequiringComment { get; } = new SettingDescriptor
                {
                    Name = "Return.ReasonsRequiringComment",
                    ValueType = SettingValueType.ShortText,
                    GroupName = "Return|Return",
                    DefaultValue = "FaultyOnArrival,DamagedInTransit,WrongItemDelivered"
                };

                /// <summary>
                /// Whether every returned line must carry at least one photo or document before the
                /// return can be submitted.
                /// </summary>
                public static SettingDescriptor ReturnAttachmentsRequired { get; } = new SettingDescriptor
                {
                    Name = "Return.AttachmentsRequired",
                    ValueType = SettingValueType.Boolean,
                    GroupName = "Return|Return",
                    DefaultValue = true,
                    // Public: the storefront disables the submit button on the same rule the server
                    // enforces, so turning this off actually unblocks the buyer.
                    IsPublic = true
                };

                public static SettingDescriptor ReturnFileUploadScopeName { get; } = new SettingDescriptor
                {
                    Name = "Return.FileUploadScopeName",
                    ValueType = SettingValueType.ShortText,
                    GroupName = "Return|Return",
                    DefaultValue = ReturnAttachmentsScope,
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
                        yield return ReturnFileUploadScopeName;
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
                    yield return General.ReturnFileUploadScopeName;
                }
            }
        }
    }
}
