using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.Return.Core
{
    public static class ModuleConstants
    {
        public static class Security
        {
            public static class Permissions
            {
                public const string Access = "Return:access";
                public const string Create = "Return:create";
                public const string Read = "Return:read";
                public const string Update = "Return:update";
                public const string Delete = "Return:delete";

                public static string[] AllPermissions { get; } = { Read, Create, Access, Update, Delete };
            }
        }

        public static class Settings
        {
            public static class General
            {
                public static SettingDescriptor ReturnEnabled { get; } = new SettingDescriptor
                {
                    Name = "Return.ReturnEnabled",
                    GroupName = "Return|General",
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

                public static IEnumerable<SettingDescriptor> AllSettings
                {
                    get
                    {
                        yield return ReturnEnabled;
                        yield return ReturnPassword;
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
        }
    }
}
