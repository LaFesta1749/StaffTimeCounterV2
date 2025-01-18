// PluginAPI imports
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using PluginAPI.Helpers;
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
        public Version Version => new Version(1, 0, 4);

        // Направи StaffMembers статична променлива
        public static Dictionary<string, StaffInfo> StaffMembers = new Dictionary<string, StaffInfo>();

        private static readonly string ConfigPath = Path.Combine(Paths.GlobalPlugins.Plugins, "StaffTimeCounterV2", "config.yml");

        [PluginEvent(ServerEventType.WaitingForPlayers)]
        public void OnWaitingForPlayers()
        {
            Log.Info($"StaffMembers loaded: {string.Join(", ", StaffTimeCounter.StaffMembers.Keys)}");
            Log.Info($"Loading StaffTimeCounterV2 - Version: {Version}");
            LoadConfig();
            if (StaffMembers != null && StaffMembers.Any())
            {
                Log.Info($"Loaded staff members: {string.Join(", ", StaffMembers.Keys)}");
                Log.Info("Config loaded successfully!");
            }
            else
            {
                Log.Warning("Config loading failed or empty!");
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    Log.Error("Config file not found at path: " + ConfigPath);
                    CreateExampleConfig();
                    return;
                }

                var deserializer = new DeserializerBuilder().Build();
                using (var reader = new StreamReader(ConfigPath))
                {
                    StaffMembers = deserializer.Deserialize<Dictionary<string, string>>(reader)
                        .ToDictionary(x => x.Key.Trim(), x => new StaffInfo { RankName = x.Value });
                }

                Log.Info($"Loaded staff members: {string.Join(", ", StaffMembers.Keys)}");
            }
            catch (Exception e)
            {
                Log.Error($"Error while loading config: {e.Message}");
            }
        }

        private void CreateExampleConfig()
        {
            var exampleConfig = new Dictionary<string, string>
    {
        { "76561198047345881@steam", "owner" }, // Примерен играч
        { "76561199048565475@steam", "head_of_staff" }
    };

            var serializer = new SerializerBuilder().Build();
            using (var writer = new StreamWriter(ConfigPath))
            {
                serializer.Serialize(writer, exampleConfig);
            }

            Log.Info("Example config.yml has been created. Please configure the plugin.");
        }
    }

    public class StaffInfo
    {
        public string RankName { get; set; } = string.Empty;
        public int ServerTime { get; set; } = 0;
        public int OverwatchTime { get; set; } = 0;
    }
}
