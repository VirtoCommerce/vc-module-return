using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ReturnModule.Core
{
    public static class ModuleConstants
    {
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
                    DefaultValue = false
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

                public static IEnumerable<SettingDescriptor> AllSettings
                {
                    get
                    {
                        yield return ReturnEnabled;
                        yield return ReturnPassword;
                        yield return ReturnNewNumberTemplate;
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
                }
            }
        }
    }
}
