// PlayerTracker.cs - Отговорен за проследяване на състоянието на играчите
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using PluginAPI.Loader.Features;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace StaffTimeCounterV2
{
    public static class PlayerTracker
    {
        private static Dictionary<string, DateTime> playerJoinTimes = new Dictionary<string, DateTime>();

        private static readonly string timesDirectory = Path.Combine(pluginDirectory, "Times");
        private static readonly string summariesDirectory = Path.Combine(timesDirectory, "Summaries");

        public static void RegisterEvents()
        {
            EventManager.RegisterEvents();
            Log.Info("PlayerTracker events registered.");
            if (!Directory.Exists(timesDirectory))
            {
                Directory.CreateDirectory(timesDirectory);
                Log.Info($"Times directory created at path: {timesDirectory}");
            }
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        public static void OnPlayerJoined(Player player)
        {
            if (ConfigManager.LoadConfig().UserRanks.ContainsKey(player.UserId))
            {
                Log.Info($"Player {player.Nickname} (ID: {player.UserId}) joined. Tracking time.");
                playerJoinTimes[player.UserId] = DateTime.Now;
            }
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        public static void OnPlayerLeft(Player player)
        {
            if (ConfigManager.LoadConfig().UserRanks.ContainsKey(player.UserId))
            {
                Log.Info($"Player {player.Nickname} (ID: {player.UserId}) left. Saving time.");
                if (playerJoinTimes.TryGetValue(player.UserId, out DateTime joinTime))
                {
                    TimeSpan timeSpent = DateTime.Now - joinTime;
                    Log.Info($"Player {player.Nickname} spent {timeSpent.TotalMinutes} minutes in the game.");
                    playerJoinTimes.Remove(player.UserId);

                    SavePlayerTime(player.UserId, player.Nickname, timeSpent);
                }
            }
        }

        private static void SavePlayerTime(string userId, string nickname, TimeSpan timeSpent)
        {
            string filePath = Path.Combine(timesDirectory, $"StaffTimeCounter_Day_{DateTime.Now:dd_MM_yyyy}.yml");
            var serializer = new SerializerBuilder().Build();

            List<TrackedTime> trackedTimes;

            if (File.Exists(filePath))
            {
                var deserializer = new DeserializerBuilder().Build();
                using (var reader = new StreamReader(filePath))
                {
                    trackedTimes = deserializer.Deserialize<List<TrackedTime>>(reader) ?? new List<TrackedTime>();
                }
            }
            else
            {
                trackedTimes = new List<TrackedTime>();
            }

            var existingEntry = trackedTimes.Find(t => t.UserId == userId);
            if (existingEntry != null)
            {
                existingEntry.ServerTime += (int)timeSpent.TotalMinutes;
            }
            else
            {
                trackedTimes.Add(new TrackedTime
                {
                    Name = nickname,
                    UserId = userId,
                    RankName = ConfigManager.LoadConfig().UserRanks[userId],
                    ServerTime = (int)timeSpent.TotalMinutes
                });
            }

            using (var writer = new StreamWriter(filePath))
            {
                serializer.Serialize(writer, trackedTimes);
            }
        }
    }

    public class TrackedTime
    {
        public string Name { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string RankName { get; set; } = string.Empty;
        public int ServerTime { get; set; }
    }
}
