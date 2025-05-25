using Exiled.API.Features;
using Exiled.Events.Handlers;
using StaffTimeCounterV2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using static RoundSummary;
using Player = Exiled.Events.Handlers.Player;

namespace StaffTimeCounterV2
{
    public class TimeTracker
    {
        private System.Timers.Timer? dayCheckTimer;
        private readonly Dictionary<string, DateTime> activeSessions = new();
        private readonly string timesDirectory;

        public TimeTracker()
        {
            timesDirectory = Path.Combine(Paths.Configs, "StaffTimeCounterV2", "Data", "Times");
            EnsureDirectoriesExist();
        }

        private void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(timesDirectory))
            {
                Directory.CreateDirectory(timesDirectory);
                Log.Debug($"[TimeTracker] Created Times directory at: {timesDirectory}");
            }
        }

        public void Register()
        {
            Player.Verified += OnPlayerVerified;
            Player.Left += OnPlayerLeft;
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;
            Log.Debug("[TimeTracker] Registered to Player events (Verified, Left, RoundEnded).");
            dayCheckTimer = new System.Timers.Timer(1000); // проверка на всяка секунда
            dayCheckTimer.Elapsed += DayCheckTimerElapsed;
            dayCheckTimer.AutoReset = true;
            dayCheckTimer.Start();
        }

        public void Unregister()
        {
            Player.Verified -= OnPlayerVerified;
            Player.Left -= OnPlayerLeft;
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;
            Log.Debug("[TimeTracker] Unregistered from Player events (Verified, Left, RoundEnded).");
            dayCheckTimer?.Stop();
            dayCheckTimer?.Dispose();

            // Запазваме всички активни сесии при изключване на плъгина
            SaveAllActiveSessions();
        }

        private void SaveAllActiveSessions()
        {
            Log.Debug("[TimeTracker] Plugin unloading - saving all active sessions...");

            foreach (var entry in activeSessions.ToList())
            {
                string userId = entry.Key;
                DateTime startTime = entry.Value;

                TimeSpan duration = DateTime.UtcNow - startTime;
                int minutesPlayed = (int)duration.TotalMinutes;
                if (duration.TotalSeconds >= 30 && minutesPlayed == 0)
                    minutesPlayed = 1;

                if (minutesPlayed > 0 && Plugin.Instance.ConfigManager.StaffMembers.TryGetValue(userId, out StaffInfo staffInfo))
                {
                    staffInfo.ServerTime += minutesPlayed;

                    var player = Exiled.API.Features.Player.List.FirstOrDefault(p => p.UserId.ToLower() == userId);
                    string playerName = player?.Nickname ?? "Unknown";

                    SaveDailyRecord(playerName, userId, staffInfo);
                    Log.Debug($"[TimeTracker] [PluginUnload] Saved record for {playerName} ({userId}) - +{minutesPlayed} min");

                    // Нулираме времето след запис
                    staffInfo.ServerTime = 0;
                }
            }

            activeSessions.Clear();
        }

        private void DayCheckTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            DateTime now = DateTime.UtcNow; // Използваме UTC време за консистентност
            DateTime localNow = DateTime.Now; // Използваме локално време само за проверка на деня от седмицата

            // Проверяваме само за Събота 23:59:59 (локално време)
            if (localNow.DayOfWeek == DayOfWeek.Saturday && localNow.Hour == 23 && localNow.Minute == 59 && localNow.Second >= 59)
            {
                Log.Debug("[TimeTracker] Saturday 23:59:59 reached. Ending current sessions before Sunday.");

                foreach (var entry in activeSessions.ToList())
                {
                    string userId = entry.Key;
                    DateTime startTime = entry.Value;
                    TimeSpan duration = now - startTime;

                    int minutesPlayed = (int)duration.TotalMinutes;
                    if (duration.TotalSeconds >= 30 && minutesPlayed == 0)
                        minutesPlayed = 1;

                    if (minutesPlayed > 0 && Plugin.Instance.ConfigManager.StaffMembers.TryGetValue(userId, out StaffInfo staffInfo))
                    {
                        staffInfo.ServerTime += minutesPlayed;

                        var player = Exiled.API.Features.Player.List.FirstOrDefault(p => p.UserId.ToLower() == userId);
                        string playerName = player?.Nickname ?? "Unknown";

                        SaveDailyRecord(playerName, userId, staffInfo);
                        Log.Debug($"[TimeTracker] [SaturdayFinal] Saved record for {playerName} ({userId}) - +{minutesPlayed} min");

                        // Нулираме времето след запис
                        staffInfo.ServerTime = 0;
                    }

                    // Рестартираме сесията за новата седмица
                    activeSessions[userId] = now;
                }
            }
        }

        private void OnRoundEnded(Exiled.Events.EventArgs.Server.RoundEndedEventArgs ev)
        {
            Log.Debug("[TimeTracker] Round ended - processing active sessions...");

            foreach (var entry in activeSessions.ToList())
            {
                string userId = entry.Key;
                DateTime startTime = entry.Value;

                TimeSpan duration = DateTime.UtcNow - startTime;
                int minutesPlayed = (int)duration.TotalMinutes;
                if (duration.TotalSeconds >= 30 && minutesPlayed == 0)
                    minutesPlayed = 1;

                if (minutesPlayed > 0 && Plugin.Instance.ConfigManager.StaffMembers.TryGetValue(userId, out StaffInfo staffInfo))
                {
                    staffInfo.ServerTime += minutesPlayed;

                    var player = Exiled.API.Features.Player.List.FirstOrDefault(p => p.UserId.ToLower() == userId);
                    string playerName = player?.Nickname ?? "Unknown";

                    SaveDailyRecord(playerName, userId, staffInfo);
                    Log.Debug($"[TimeTracker] [RoundEnd] Saved record for {playerName} ({userId}) - +{minutesPlayed} min");

                    // Нулираме времето след запис
                    staffInfo.ServerTime = 0;
                }

                // Обновяваме началното време на сесията вместо да я премахваме
                activeSessions[userId] = DateTime.UtcNow;
            }
        }

        private void OnPlayerVerified(Exiled.Events.EventArgs.Player.VerifiedEventArgs ev)
        {
            if (Plugin.Instance?.ConfigManager?.StaffMembers == null)
            {
                Log.Debug("[TimeTracker] ConfigManager or StaffMembers is null! Skipping tracking on verify.");
                return;
            }

            if (ev.Player == null || string.IsNullOrWhiteSpace(ev.Player.UserId))
            {
                Log.Debug("[TimeTracker] Player has no UserId on verify! Skipping tracking.");
                return;
            }

            string userId = ev.Player.UserId.ToLower();

            if (Plugin.Instance.ConfigManager.StaffMembers.ContainsKey(userId))
            {
                activeSessions[userId] = DateTime.UtcNow; // Използваме UTC време
                Log.Debug($"[TimeTracker] Started tracking {ev.Player.Nickname} ({ev.Player.UserId}).");
            }
            else
            {
                Log.Debug($"[TimeTracker] {ev.Player.Nickname} ({ev.Player.UserId}) is not listed in StaffMembers, skipping tracking.");
            }
        }

        private void OnPlayerLeft(Exiled.Events.EventArgs.Player.LeftEventArgs ev)
        {
            if (Plugin.Instance?.ConfigManager?.StaffMembers == null)
            {
                Log.Debug("[TimeTracker] ConfigManager or StaffMembers is null! Skipping tracking on leave.");
                return;
            }

            if (ev.Player == null || string.IsNullOrWhiteSpace(ev.Player.UserId))
                return;

            string userId = ev.Player.UserId.ToLower();

            if (activeSessions.TryGetValue(userId, out DateTime startTime))
            {
                TimeSpan duration = DateTime.UtcNow - startTime; // Използваме UTC време

                int minutesPlayed = (int)duration.TotalMinutes;
                if (duration.TotalSeconds >= 30 && minutesPlayed == 0)
                    minutesPlayed = 1;

                if (minutesPlayed > 0 && Plugin.Instance.ConfigManager.StaffMembers.TryGetValue(userId, out StaffInfo staffInfo))
                {
                    staffInfo.ServerTime += minutesPlayed;
                    SaveDailyRecord(ev.Player.Nickname, ev.Player.UserId, staffInfo);

                    // Нулираме времето след запис
                    staffInfo.ServerTime = 0;

                    Log.Debug($"[TimeTracker] Stopped tracking {ev.Player.Nickname} ({ev.Player.UserId}), played {minutesPlayed} minutes.");
                }
                else
                {
                    Log.Debug($"[TimeTracker] {ev.Player.Nickname} played less than 30 seconds or not found in StaffMembers. Skipping save.");
                }

                activeSessions.Remove(userId);
            }
            else
            {
                Log.Debug($"[TimeTracker] No active session found for {ev.Player.Nickname} ({ev.Player.UserId}).");
            }
        }

        private void SaveDailyRecord(string playerName, string userId, StaffInfo staffInfo)
        {
            try
            {
                // Използваме локално време за името на файла, за да съответства на деня
                string todayDate = DateTime.Now.ToString("dd_MM_yyyy");
                string fileName = $"StaffTimeCounter_Day_{todayDate}.yml";
                string filePath = Path.Combine(timesDirectory, fileName);

                List<DailyRecord> dailyRecords = new();

                if (File.Exists(filePath))
                {
                    var deserializer = new DeserializerBuilder().Build();
                    using var reader = new StreamReader(filePath);
                    dailyRecords = deserializer.Deserialize<List<DailyRecord>>(reader) ?? new List<DailyRecord>();
                    Log.Debug($"[TimeTracker] Loaded existing daily records ({dailyRecords.Count}) from {filePath}.");
                }

                var existingRecord = dailyRecords.FirstOrDefault(r => r.UserId == userId);
                if (existingRecord != null)
                {
                    existingRecord.ServerTime += staffInfo.ServerTime;
                    existingRecord.OverwatchTime += staffInfo.OverwatchTime;
                    Log.Debug($"[TimeTracker] Updated record for {playerName} ({userId}). Total time: {existingRecord.ServerTime} minutes.");
                }
                else
                {
                    dailyRecords.Add(new DailyRecord
                    {
                        Name = playerName,
                        UserId = userId,
                        RankName = staffInfo.RankName,
                        ServerTime = staffInfo.ServerTime,
                        OverwatchTime = staffInfo.OverwatchTime
                    });
                    Log.Debug($"[TimeTracker] Added new record for {playerName} ({userId}). Time: {staffInfo.ServerTime} minutes.");
                }

                var serializer = new SerializerBuilder().Build();
                using var writer = new StreamWriter(filePath, false); // overwrite file
                serializer.Serialize(writer, dailyRecords);
                writer.Flush();

                Log.Debug($"[TimeTracker] Successfully saved daily records to {filePath}.");
            }
            catch (Exception ex)
            {
                Log.Debug($"[TimeTracker] Failed to save daily record for {playerName} ({userId}): {ex}");
            }
        }
    }
}
