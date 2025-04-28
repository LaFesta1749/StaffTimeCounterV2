using Exiled.API.Features;
using Exiled.Events.Handlers;
using StaffTimeCounterV2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using Player = Exiled.Events.Handlers.Player;

namespace StaffTimeCounterV2
{
    public class TimeTracker
    {
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
            Log.Debug("[TimeTracker] Registered to Player events (Verified, Left).");
        }

        public void Unregister()
        {
            Player.Verified -= OnPlayerVerified;
            Player.Left -= OnPlayerLeft;
            Log.Debug("[TimeTracker] Unregistered from Player events (Verified, Left).");
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
                activeSessions[userId] = DateTime.UtcNow;
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
                TimeSpan duration = DateTime.UtcNow - startTime;

                int minutesPlayed = (int)duration.TotalMinutes;
                if (duration.TotalSeconds >= 30 && minutesPlayed == 0)
                    minutesPlayed = 1;

                if (minutesPlayed > 0 && Plugin.Instance.ConfigManager.StaffMembers.TryGetValue(userId, out StaffInfo staffInfo))
                {
                    staffInfo.ServerTime += minutesPlayed;
                    SaveDailyRecord(ev.Player.Nickname, ev.Player.UserId, staffInfo);
                }
                else
                {
                    Log.Debug($"[TimeTracker] {ev.Player.Nickname} played less than 30 seconds. Skipping save.");
                }

                activeSessions.Remove(userId);
                Log.Debug($"[TimeTracker] Stopped tracking {ev.Player.Nickname} ({ev.Player.UserId}), played {minutesPlayed} minutes.");
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
                    existingRecord.ServerTime = staffInfo.ServerTime;
                    existingRecord.OverwatchTime = staffInfo.OverwatchTime;
                    Log.Debug($"[TimeTracker] Updated record for {playerName} ({userId}).");
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
                    Log.Debug($"[TimeTracker] Added new record for {playerName} ({userId}).");
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
