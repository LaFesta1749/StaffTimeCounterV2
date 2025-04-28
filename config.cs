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
        public string Name => "StaffTimeCounterV2";
        public string Author => "LaFesta1749";
        public Version Version => new Version(1, 0, 6);

        public static Dictionary<string, StaffInfo> StaffMembers = new Dictionary<string, StaffInfo>();

        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "StaffTimeCounterV2", "config.yml");

        public void Load()
        {
            Logger.Info($"Loading StaffTimeCounterV2 - Version: {Version}");

            if (!File.Exists(ConfigPath))
            {
                Logger.Error("Config file not found. Creating example config...");
                CreateExampleConfig();
                return;
            }

            try
            {
                var deserializer = new DeserializerBuilder().Build();
                using (var reader = new StreamReader(ConfigPath))
                {
                    StaffMembers = deserializer.Deserialize<Dictionary<string, string>>(reader)
                        .ToDictionary(x => x.Key.Trim(), x => new StaffInfo { RankName = x.Value });
                }

                Logger.Info($"Loaded staff members: {string.Join(", ", StaffMembers.Keys)}");
            }
            catch (Exception e)
            {
                Logger.Error($"Error while loading config: {e.Message}");
            }
        }

        private void CreateExampleConfig()
        {
            try
            {
                var exampleConfig = new Dictionary<string, string>
                {
                    { "76561198047345881@steam", "owner" }, // Example staff
                    { "76561199048565475@steam", "head_of_staff" }
                };

                var serializer = new SerializerBuilder().Build();
                using (var writer = new StreamWriter(ConfigPath))
                {
                    serializer.Serialize(writer, exampleConfig);
                }

                Logger.Info("Example config.yml has been created. Please configure the plugin.");
            }
            catch (Exception e)
            {
                Logger.Error($"Error while creating example config: {e.Message}");
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
