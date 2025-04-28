using LabApi.Features.Console;
using LabApi.Loader.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace StaffTimeCounterV2
{
    public class StaffTimeCounter
    {
        public static Dictionary<string, StaffInfo> StaffMembers { get; private set; } = new();

        private static readonly string BaseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "StaffTimeCounterV2");
        private static readonly string ConfigPath = Path.Combine(BaseDirectory, "config.yml");

        public void Load()
        {
            Logger.Info("Loading StaffTimeCounterV2 configuration...");

            try
            {
                // Ensure base directory exists
                if (!Directory.Exists(BaseDirectory))
                {
                    Directory.CreateDirectory(BaseDirectory);
                    Logger.Info($"Created plugin base directory: {BaseDirectory}");
                }

                if (!File.Exists(ConfigPath))
                {
                    Logger.Warn("Config.yml not found. Creating default example config...");
                    CreateExampleConfig();
                    Logger.Warn("Please configure your staff members and restart the server.");
                    return;
                }

                var deserializer = new DeserializerBuilder().Build();
                using var reader = new StreamReader(ConfigPath);

                var rawConfig = deserializer.Deserialize<Dictionary<string, string>>(reader);

                if (rawConfig == null || rawConfig.Count == 0)
                {
                    Logger.Warn("Config.yml is empty or invalid. No staff members loaded.");
                    return;
                }

                StaffMembers = rawConfig
                    .ToDictionary(x => x.Key.Trim().ToLower(), x => new StaffInfo { RankName = x.Value });

                Logger.Info($"Loaded {StaffMembers.Count} staff members: {string.Join(", ", StaffMembers.Keys)}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading config.yml: {ex.Message}");
            }
        }

        private void CreateExampleConfig()
        {
            try
            {
                var exampleConfig = new Dictionary<string, string>
                {
                    { "76561198047345881@steam", "owner" },
                    { "76561199048565475@steam", "head_of_staff" }
                };

                var serializer = new SerializerBuilder().Build();
                using var writer = new StreamWriter(ConfigPath);
                serializer.Serialize(writer, exampleConfig);

                Logger.Info("Example config.yml created successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating example config.yml: {ex.Message}");
            }
        }
    }

    public class StaffInfo
    {
        public string RankName { get; set; } = string.Empty;
        public int ServerTime { get; set; } = 0;
        public int OverwatchTime { get; set; } = 0;
    }
}
