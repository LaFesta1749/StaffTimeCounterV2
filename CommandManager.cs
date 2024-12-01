// CommandManager.cs - Отговорен за регистрацията на команди и обобщение на времето на играчите
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace StaffTimeCounterV2
{
    public static class CommandManager
    {
        private static readonly string timesDirectory = Path.Combine(pluginDirectory, "Times");
        private static readonly string pluginDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static readonly string summariesDirectory = Path.Combine(timesDirectory, "Summaries");

        public static void RegisterCommands()
        {
            Log.Info("Registering commands for StaffTimeCounterV2...");
            if (!Directory.Exists(timesDirectory))
            {
                Directory.CreateDirectory(timesDirectory);
            }
            if (!Directory.Exists(summariesDirectory))
            {
                Directory.CreateDirectory(summariesDirectory);
                Log.Info($"Summaries directory created at path: {summariesDirectory}");
            }
        }

        [PluginCommand("stc_summary", "Generates a summary of player times.", CommandType.RemoteAdmin)]
        public static void GenerateSummary()
        {
            var startDate = Directory.GetFiles(timesDirectory, "StaffTimeCounter_Day_*.yml").Select(Path.GetFileNameWithoutExtension).Select(f => f.Replace("StaffTimeCounter_Day_", "")).OrderBy(f => f).FirstOrDefault();
            var endDate = Directory.GetFiles(timesDirectory, "StaffTimeCounter_Day_*.yml").Select(Path.GetFileNameWithoutExtension).Select(f => f.Replace("StaffTimeCounter_Day_", "")).OrderBy(f => f).LastOrDefault();
            var summaryFilePath = Path.Combine(summariesDirectory, $"{startDate} --- {endDate}.yml");
            var serializer = new SerializerBuilder().Build();
            List<TrackedTime> summaryTrackedTimes = new List<TrackedTime>();

            foreach (var filePath in Directory.GetFiles(timesDirectory, "StaffTimeCounter_Day_*.yml"))
            {
                if (File.Exists(filePath))
                {
                    var deserializer = new DeserializerBuilder().Build();
                    using (var reader = new StreamReader(filePath))
                    {
                        var trackedTimes = deserializer.Deserialize<List<TrackedTime>>(reader) ?? new List<TrackedTime>();
                        foreach (var trackedTime in trackedTimes)
                        {
                            var existingEntry = summaryTrackedTimes.FirstOrDefault(t => t.UserId == trackedTime.UserId);
                            if (existingEntry != null)
                            {
                                existingEntry.ServerTime += trackedTime.ServerTime;
                            }
                            else
                            {
                                summaryTrackedTimes.Add(new TrackedTime
                                {
                                    Name = trackedTime.Name,
                                    UserId = trackedTime.UserId,
                                    RankName = trackedTime.RankName,
                                    ServerTime = trackedTime.ServerTime
                                });
                            }
                        }
                    }
                }
            }

            using (var writer = new StreamWriter(summaryFilePath))
            {
                serializer.Serialize(writer, summaryTrackedTimes);
            }

            Log.Info($"Summary generated for the period {startDate} to {endDate}.");
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
