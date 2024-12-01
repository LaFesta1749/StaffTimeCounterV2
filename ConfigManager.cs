// ConfigManager.cs - Отговорен за управление на конфигурационния файл
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Collections.Generic;
using PluginAPI.Core;
using PluginAPI.Loader.Features;

namespace StaffTimeCounterV2
{
    public static class ConfigManager
    {
        public static Config LoadConfig()
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

            var configPath = Path.Combine(pluginDirectory, "config.yml");
            if (!File.Exists(configPath))
            {
                Log.Info($"Config file not found at expected path: {configPath}. Generating default config.");
                var defaultConfig = new Config();
                var serializer = new SerializerBuilder().Build();
                using (var writer = new StreamWriter(configPath))
                {
                    serializer.Serialize(writer, defaultConfig);
                }
                Log.Info("Default config file generated.");
                return new Config();
            }

            using (var reader = new StreamReader(configPath))
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