// LabAPI imports
using LabApi.Events.Arguments;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Console;
using LabApi.Loader.Features;
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
        private static readonly string TimesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "StaffTimeCounterV2", "Times");
        private Dictionary<string, StaffInfo> staffMembers;
        private Dictionary<string, DateTime> activeSessions = new Dictionary<string, DateTime>();

        public TimeTracker(Dictionary<string, StaffInfo> staffMembers)
        {
            this.staffMembers = staffMembers;
            EventManager.RegisterEvents(this); // <-- ТУК регистрираш събитията
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        public void OnPlayerJoined(PlayerJoinedEventArgs ev)
        {
            Logger.Info($"Player joined: {ev.Player.UserId}");

            if (StaffTimeCounter.StaffMembers.ContainsKey(ev.Player.UserId.ToLower()))
            {
                Logger.Info($"Player {ev.Player.Nickname} ({ev.Player.UserId}) is being tracked.");
                activeSessions[ev.Player.UserId] = DateTime.Now;
            }
            else
            {
                Logger.Info($"Player {ev.Player.Nickname} ({ev.Player.UserId}) is not being tracked.");
            }
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        public void OnPlayerLeft(PlayerLeftEventArgs ev)
        {
            if (ev.Player?.UserId == null)
            {
                Logger.Warn("Player UserId is null. Skipping player leave tracking.");
                return;
            }
            if (activeSessions.ContainsKey(ev.Player.UserId))
            {
                DateTime joinTime = activeSessions[ev.Player.UserId];
                TimeSpan sessionDuration = DateTime.Now - joinTime;
                Logger.Info($"Tracking end time for player {ev.Player.Nickname} ({ev.Player.UserId}) - Session duration: {sessionDuration.TotalMinutes} minutes");

                if (StaffTimeCounter.StaffMembers.TryGetValue(ev.Player.UserId.ToLower(), out StaffInfo staffInfo))
                {
                    Logger.Info($"ServerTime before adding session duration: {staffInfo.ServerTime}");
                    staffInfo.ServerTime += (int)sessionDuration.TotalMinutes;
                    Logger.Info($"ServerTime after adding session duration: {staffInfo.ServerTime}");

                    SaveDailyTime(ev.Player.UserId, ev.Player.Nickname, staffInfo);
                }
                activeSessions.Remove(ev.Player.UserId);
            }
        }

        private void SaveDailyTime(string userId, string playerName, StaffInfo staffInfo)
        {
            try
            {
                Logger.Info($"Saving daily time for player {playerName} ({userId})");

                if (!Directory.Exists(TimesPath))
                {
                    Directory.CreateDirectory(TimesPath);
                    Logger.Info($"Created Times directory at path: {TimesPath}");
                }

                string fileName = $"StaffTimeCounter_Day_{DateTime.Now:dd_MM_yyyy}.yml";
                string filePath = Path.Combine(TimesPath, fileName);

                Logger.Info($"Saving data to file: {filePath}");

                List<DailyRecord> dailyRecords = new List<DailyRecord>();
                if (File.Exists(filePath))
                {
                    Logger.Info($"File {filePath} already exists. Loading existing records...");
                    var deserializer = new DeserializerBuilder().Build();
                    using (var reader = new StreamReader(filePath))
                    {
                        dailyRecords = deserializer.Deserialize<List<DailyRecord>>(reader) ?? new List<DailyRecord>();
                    }
                }

                var existingRecord = dailyRecords.FirstOrDefault(x => x.UserId == userId);
                if (existingRecord != null)
                {
                    Logger.Info($"Updating existing record for player {playerName} ({userId})");
                    existingRecord.ServerTime += (int)(DateTime.Now - activeSessions[userId]).TotalMinutes;
                }
                else
                {
                    Logger.Info($"Creating new record for player {playerName} ({userId})");
                    dailyRecords.Add(new DailyRecord
                    {
                        Name = playerName,
                        UserId = userId,
                        RankName = staffInfo.RankName,
                        ServerTime = (int)(DateTime.Now - activeSessions[userId]).TotalMinutes,
                        OverwatchTime = staffInfo.OverwatchTime
                    });
                }

                var serializer = new SerializerBuilder().Build();
                using (var writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, dailyRecords);
                    Logger.Info($"Successfully saved data for player {playerName} ({userId})");
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error while saving daily time for player {playerName} ({userId}): {e.Message}");
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
