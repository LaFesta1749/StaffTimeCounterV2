using Exiled.API.Features;
using StaffTimeCounterV2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace StaffTimeCounterV2
{
    public class ConfigManager
    {
        public Dictionary<string, StaffInfo> StaffMembers { get; private set; } = new();

        private readonly string configDirectory;
        private readonly string configFilePath;

        public ConfigManager()
        {
            configDirectory = Path.Combine(Paths.Configs, "StaffTimeCounterV2", "Config");
            configFilePath = Path.Combine(configDirectory, "config.yml");

            EnsureDirectoriesExist();
        }

        private void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
                Log.Debug($"Created config directory at: {configDirectory}");
            }
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(configFilePath))
                {
                    CreateDefaultConfig();
                    Log.Debug("No config.yml found. A default one has been created. Please configure it and reload.");
                    return;
                }

                var deserializer = new DeserializerBuilder().Build();
                using var reader = new StreamReader(configFilePath);

                var rawConfig = deserializer.Deserialize<Dictionary<string, string>>(reader);

                if (rawConfig == null || rawConfig.Count == 0)
                {
                    Log.Debug("Config file is empty or invalid. No staff members loaded.");
                    return;
                }

                StaffMembers = rawConfig
                    .ToDictionary(x => x.Key.Trim().ToLower(), x => new StaffInfo { RankName = x.Value });

                Log.Debug($"Loaded {StaffMembers.Count} staff members: {string.Join(", ", StaffMembers.Keys)}");
            }
            catch (Exception ex)
            {
                Log.Debug($"Error loading config.yml: {ex.Message}");
            }
        }

        private void CreateDefaultConfig()
        {
            try
            {
                var exampleConfig = new Dictionary<string, string>
                {
                    { "76561198047345881@steam", "Owner" },
                    { "76561199048565475@steam", "Head_Of_Staff" }
                };

                var serializer = new SerializerBuilder().Build();
                using var writer = new StreamWriter(configFilePath);
                serializer.Serialize(writer, exampleConfig);

                Log.Debug("Default config.yml created successfully.");
            }
            catch (Exception ex)
            {
                Log.Debug($"Error creating default config.yml: {ex.Message}");
            }
        }
    }
}
