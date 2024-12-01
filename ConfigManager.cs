// ConfigManager.cs - Отговорен за управление на конфигурационния файл
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Collections.Generic;
using PluginAPI.Core;

namespace StaffTimeCounterV2
{
    public static class ConfigManager
    {
        public static Config LoadConfig()
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            if (!File.Exists("plugins/7777/StaffTimeCounterV2/config.yml"))
            {
                Log.Error("Config file not found at expected path: plugins/7777/StaffTimeCounterV2/config.yml");
                return new Config();
            }

            using (var reader = new StreamReader("plugins/7777/StaffTimeCounterV2/config.yml"))
            {
                var config = deserializer.Deserialize<Config>(reader) ?? new Config();
                Log.Info("Config successfully loaded.");
                return config;
            }
        }
    }

    public class Config
    {
        public Dictionary<string, string> UserRanks { get; set; } = new Dictionary<string, string>();
    }
}
