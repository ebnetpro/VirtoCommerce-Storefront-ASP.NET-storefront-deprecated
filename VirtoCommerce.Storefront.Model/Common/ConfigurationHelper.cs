using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace VirtoCommerce.Storefront.Model.Common
{
    public class ConfigurationHelper
    {
        public const string ConnectionStringPrefix = "VIRTO_CONN_STR_";
        public const string AppSettingPrefix = "VIRTO_APP_SETTING_";

        public static string GetConnectionStringValue(string nameOrConnectionString)
        {
            if (nameOrConnectionString == null)
                throw new ArgumentNullException(nameof(nameOrConnectionString));

            var result = nameOrConnectionString;

            if (nameOrConnectionString.IndexOf('=') < 0)
            {
                result = Environment.GetEnvironmentVariable($"{ConnectionStringPrefix}{nameOrConnectionString}");

                if (string.IsNullOrEmpty(result))
                {
                    result = ConfigurationManager.ConnectionStrings[nameOrConnectionString]?.ConnectionString;
                }
            }

            return result;
        }

        public static string GetAppSettingsValue(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return GetAppSettingsValue<string>(name, null);
        }

        public static T GetAppSettingsValue<T>(string name, T defaultValue)
            where T : IConvertible
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var value = Environment.GetEnvironmentVariable($"{AppSettingPrefix}{name}");

            if (value == null && ConfigurationManager.AppSettings.AllKeys.Contains(name))
            {
                value = ConfigurationManager.AppSettings[name];
            }

            var result = value != null
                ? (T)Convert.ChangeType(value, typeof(T))
                : defaultValue;

            return result;
        }

        public static IList<string> GetAppSettingsNames()
        {
            var names = new List<string>(ConfigurationManager.AppSettings.AllKeys);

            var environmentVariables = Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry de in environmentVariables)
            {
                var name = (string)de.Key;
                if (name.StartsWith(AppSettingPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(AppSettingPrefix.Length);
                    names.Add(name);
                }
            }

            var result = names
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return result;
        }
    }
}
