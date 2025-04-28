using CommandSystem;
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
    public class SummaryGenerator
    {
        private static readonly string BaseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "StaffTimeCounterV2");
        private static readonly string TimesPath = Path.Combine(BaseDirectory, "Times");
        private static readonly string SummariesPath = Path.Combine(BaseDirectory, "Summaries");
        private static readonly string ConfigPath = Path.Combine(BaseDirectory, "config.yml");

        public SummaryGenerator()
        {
            if (!Directory.Exists(SummariesPath))
            {
                Directory.CreateDirectory(SummariesPath);
            }

            LoadConfig();
        }

        [PluginEntryPoint("StaffTimeCounterV2", "1.0.0", "Tracks staff playtime and generates reports.", "LaFesta1749")]
        public void OnStart()
        {
            Logger.Info("StaffTimeCounterV2 plugin has been loaded.");
            EventManager.RegisterEvents(this);
            EventManager.RegisterEvents(new TimeTracker(StaffTimeCounter.StaffMembers));
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    Logger.Warn("Config file not found. Creating example config...");
                    CreateExampleConfig();
                    return;
                }

                var deserializer = new DeserializerBuilder().Build();
                using (var reader = new StreamReader(ConfigPath))
                {
                    StaffTimeCounter.StaffMembers = deserializer.Deserialize<Dictionary<string, string>>(reader)
                        .ToDictionary(x => x.Key.Trim(), x => new StaffInfo { RankName = x.Value });
                }

                Logger.Info($"Loaded staff members: {string.Join(", ", StaffTimeCounter.StaffMembers.Keys)}");
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
                    { "76561198047345881@steam", "owner" },
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

        public void OnSummaryCommandWithLogging()
        {
            try
            {
                Logger.Info($"Using TimesPath: {TimesPath}");

                if (!Directory.Exists(TimesPath))
                {
                    Logger.Warn($"The Times directory does not exist: {TimesPath}");
                    return;
                }

                var files = Directory.GetFiles(TimesPath, "StaffTimeCounter_Day_*.yml");
                if (files.Length == 0)
                {
                    Logger.Warn("No files found in Times directory to process.");
                    return;
                }

                List<DailyRecord> combinedRecords = new List<DailyRecord>();
                DateTime? startDate = null;
                DateTime? endDate = null;
                DateTime today = DateTime.Today;

                foreach (var filePath in files)
                {
                    Logger.Info($"Processing file: {filePath}");
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string datePart = fileName.Replace("StaffTimeCounter_Day_", "");
                    if (!DateTime.TryParseExact(datePart, "dd_MM_yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fileDate))
                    {
                        Logger.Warn($"Could not parse date from file: {filePath}");
                        continue;
                    }

                    if (fileDate == today)
                    {
                        Logger.Info($"Skipping today's file: {filePath}");
                        continue;
                    }

                    if (startDate == null || fileDate < startDate) startDate = fileDate;
                    if (endDate == null || fileDate > endDate) endDate = fileDate;

                    var deserializer = new DeserializerBuilder().Build();
                    using (var reader = new StreamReader(filePath))
                    {
                        var dailyRecords = deserializer.Deserialize<List<DailyRecord>>(reader) ?? new List<DailyRecord>();
                        foreach (var record in dailyRecords)
                        {
                            var existingRecord = combinedRecords.FirstOrDefault(x => x.UserId == record.UserId);
                            if (existingRecord != null)
                            {
                                existingRecord.ServerTime += record.ServerTime;
                                existingRecord.OverwatchTime += record.OverwatchTime;
                            }
                            else
                            {
                                combinedRecords.Add(new DailyRecord
                                {
                                    Name = record.Name,
                                    UserId = record.UserId,
                                    RankName = record.RankName,
                                    ServerTime = record.ServerTime,
                                    OverwatchTime = record.OverwatchTime
                                });
                            }
                        }
                    }

                    File.Delete(filePath);
                }

                if (startDate != null && endDate != null)
                {
                    SaveSummary(startDate.Value, endDate.Value, combinedRecords);
                }
                else
                {
                    Logger.Warn("No valid records found to generate a summary.");
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error generating summary: {e.Message}");
            }
        }

        private void SaveSummary(DateTime start, DateTime end, List<DailyRecord> combinedRecords)
        {
            string summaryFileName = $"{start:dd_MM_yyyy} --- {end:dd_MM_yyyy}.yml";
            string summaryFilePath = Path.Combine(SummariesPath, summaryFileName);

            var serializer = new SerializerBuilder().Build();
            using (var writer = new StreamWriter(summaryFilePath))
            {
                var formattedRecords = combinedRecords.Select(record => new
                {
                    record.Name,
                    record.UserId,
                    record.RankName,
                    ServerTime = $"\"{record.ServerTime / 60:D2}:{record.ServerTime % 60:D2}\"",
                    OverwatchTime = $"\"{record.OverwatchTime / 60:D2}:{record.OverwatchTime % 60:D2}\""
                }).ToList();

                serializer.Serialize(writer, formattedRecords);
            }

            Logger.Info($"Summary saved at: {summaryFilePath}");
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class SummaryCommandHandler : ICommand
    {
        public string Command { get; } = "stc_summary";
        public string[] Aliases { get; } = new[] { "stafftimecounter_summary" };
        public string Description { get; } = "Generates a summary of staff members' playtime.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count == 0)
            {
                response = "Usage: stc_summary admin OR stc_summary user";
                return false;
            }

            string summaryType = arguments.At(0).ToLower();

            if (summaryType == "admin" || summaryType == "user")
            {
                new SummaryGenerator().OnSummaryCommandWithLogging();
                response = "Summary report generated successfully.";
                return true;
            }

            response = "Invalid summary type. Use 'admin' or 'user'.";
            return false;
        }
    }
}
