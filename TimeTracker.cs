// PluginAPI imports
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace StaffTimeCounterV2
{
    public class TimeTracker
    {
        private static readonly string TimesPath = @".config/SCP Secret Laboratory/PluginAPI/plugins/27049/StaffTimeCounterV2/Times";
        private Dictionary<string, StaffInfo> staffMembers;
        private Dictionary<string, DateTime> activeSessions = new Dictionary<string, DateTime>();

        public TimeTracker(Dictionary<string, StaffInfo> staffMembers)
        {
            this.staffMembers = staffMembers;
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        public void OnPlayerJoined(Player player)
        {
            Log.Info($"Player joined: {player.UserId}");

            // Проверка дали играчът е в списъка за следене
            if (StaffTimeCounter.StaffMembers.ContainsKey(player.UserId.ToLower()))
            {
                Log.Info($"Player {player.Nickname} ({player.UserId}) is being tracked.");
                // Добави играча в activeSessions със сегашното време
                activeSessions[player.UserId] = DateTime.Now;
            }
            else
            {
                Log.Info($"Player {player.Nickname} ({player.UserId}) is not being tracked.");
            }
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        public void OnPlayerLeft(Player player)
        {
            if (activeSessions.ContainsKey(player.UserId))
            {
                DateTime joinTime = activeSessions[player.UserId];
                TimeSpan sessionDuration = DateTime.Now - joinTime;
                Log.Info($"Tracking end time for player {player.Nickname} ({player.UserId}) - Session duration: {sessionDuration.TotalMinutes} minutes");

                if (StaffTimeCounter.StaffMembers.TryGetValue(player.UserId.ToLower(), out StaffInfo staffInfo))
                {
                    Log.Info($"ServerTime before adding session duration: {staffInfo.ServerTime}");
                    staffInfo.ServerTime += (int)sessionDuration.TotalMinutes;
                    Log.Info($"ServerTime after adding session duration: {staffInfo.ServerTime}");

                    SaveDailyTime(player.UserId, player.Nickname, staffInfo);
                }
                activeSessions.Remove(player.UserId);
            }
        }

        private void SaveDailyTime(string userId, string playerName, StaffInfo staffInfo)
        {
            try
            {
                Log.Info($"Saving daily time for player {playerName} ({userId})");

                if (!Directory.Exists(TimesPath))
                {
                    Directory.CreateDirectory(TimesPath);
                    Log.Info($"Created Times directory at path: {TimesPath}");
                }

                string fileName = $"StaffTimeCounter_Day_{DateTime.Now:dd_MM_yyyy}.yml";
                string filePath = Path.Combine(TimesPath, fileName);

                Log.Info($"Saving data to file: {filePath}");

                List<DailyRecord> dailyRecords = new List<DailyRecord>();
                if (File.Exists(filePath))
                {
                    Log.Info($"File {filePath} already exists. Loading existing records...");
                    var deserializer = new DeserializerBuilder().Build();
                    using (var reader = new StreamReader(filePath))
                    {
                        dailyRecords = deserializer.Deserialize<List<DailyRecord>>(reader) ?? new List<DailyRecord>();
                    }
                }

                var existingRecord = dailyRecords.FirstOrDefault(x => x.UserId == userId);
                if (existingRecord != null)
                {
                    Log.Info($"Updating existing record for player {playerName} ({userId})");
                    existingRecord.ServerTime += staffInfo.ServerTime;
                }
                else
                {
                    Log.Info($"Creating new record for player {playerName} ({userId})");
                    dailyRecords.Add(new DailyRecord
                    {
                        Name = playerName,
                        UserId = userId,
                        RankName = staffInfo.RankName,
                        ServerTime = staffInfo.ServerTime,
                        OverwatchTime = staffInfo.OverwatchTime
                    });
                }

                Log.Info($"Writing data to file: {filePath}");
                var serializer = new SerializerBuilder().Build();
                using (var writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, dailyRecords);
                    Log.Info($"Successfully saved data for player {playerName} ({userId})");
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error while saving daily time for player {playerName} ({userId}): {e.Message}");
            }
        }
    }

    public class DailyRecord
    {
        public string Name { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string RankName { get; set; } = string.Empty;
        public int ServerTime { get; set; } = 0;
        public int OverwatchTime { get; set; } = 0;
    }
}
