// PluginAPI imports
using CommandSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
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
        private static readonly string BaseDirectory = @".config/SCP Secret Laboratory/PluginAPI/plugins/27049/StaffTimeCounterV2";
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
            PluginAPI.Core.Log.Info("StaffTimeCounterV2 plugin has been loaded.");
            EventManager.RegisterEvents(this); // Регистрация на събитията в този клас
            EventManager.RegisterEvents(new TimeTracker(StaffTimeCounter.StaffMembers)); // Регистрация на TimeTracker с StaffMembers
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    Log.Warning("Config file not found at path: " + ConfigPath);
                    CreateExampleConfig();
                    return;
                }

                var deserializer = new DeserializerBuilder().Build();
                using (var reader = new StreamReader(ConfigPath))
                {
                    StaffTimeCounter.StaffMembers = deserializer.Deserialize<Dictionary<string, string>>(reader)
                        .ToDictionary(x => x.Key.Trim(), x => new StaffInfo { RankName = x.Value });
                }

                Log.Info($"Loaded staff members: {string.Join(", ", StaffTimeCounter.StaffMembers.Keys)}");
            }
            catch (Exception e)
            {
                Log.Error($"Error while loading config: {e.Message}");
            }
        }

        private void CreateExampleConfig()
        {
            try
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
            catch (Exception e)
            {
                Log.Error($"Error while creating example config: {e.Message}");
            }
        }

        public void OnSummaryCommandWithLogging()
        {
            List<DailyRecord> dailyRecords = new List<DailyRecord>();
            try
            {
                Log.Info($"Using TimesPath: {TimesPath}");
                if (!Directory.Exists(TimesPath))
                {
                    Log.Warning($"The Times directory does not exist: {TimesPath}");
                    return;
                }
                var files = Directory.GetFiles(TimesPath, "StaffTimeCounter_Day_*.yml");
                if (files.Length == 0)
                {
                    Log.Warning("No files found in Times directory to process.");
                    return;
                }
                List<DailyRecord> combinedRecords = new List<DailyRecord>();
                DateTime? startDate = null;
                DateTime? endDate = null;

                DateTime today = DateTime.Today;
                foreach (var filePath in files)
                {
                    Log.Info($"Processing file: {filePath}");
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string datePart = fileName.Replace("StaffTimeCounter_Day_", "");
                    DateTime fileDate;
                    if (!DateTime.TryParseExact(datePart, "dd_MM_yyyy", null, System.Globalization.DateTimeStyles.None, out fileDate))
                    {
                        Log.Warning($"Could not parse date from file: {filePath}");
                        continue;
                    }
                    // Пропусни днешния файл
                    if (fileDate == today)
                    {
                        Log.Info($"Skipping today's file: {filePath}");
                        continue;
                    }

                    if (startDate == null || fileDate < startDate)
                    {
                        startDate = fileDate;
                    }
                    if (endDate == null || fileDate > endDate)
                    {
                        endDate = fileDate;
                    }

                    var deserializer = new DeserializerBuilder().Build();
                    using (var reader = new StreamReader(filePath))
                    {
                        try
                        {
                            dailyRecords = deserializer.Deserialize<List<DailyRecord>>(reader);
                            if (dailyRecords == null)
                            {
                                Log.Warning($"No records found in file: {filePath}");
                                continue;
                            }
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
                        catch (Exception ex)
                        {
                            Log.Error($"Error deserializing file {filePath}: {ex.Message}");
                            continue;
                        }
                    }

                    // Delete processed file
                    File.Delete(filePath);
                }

                if (startDate != null && endDate != null)
                {
                    SaveSummary(startDate.Value, endDate.Value, combinedRecords);
                }
                else
                {
                    Log.Warning("No records found to generate a summary.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error generating summary: {ex.Message}");
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
                    ServerTime = string.Format("{0:D2}:{1:D2}", record.ServerTime / 60, record.ServerTime % 60),
                    OverwatchTime = string.Format("{0:D2}:{1:D2}", record.OverwatchTime / 60, record.OverwatchTime % 60)
                }).ToList();
                serializer.Serialize(writer, formattedRecords);
            }

            Log.Info($"Summary saved at: {summaryFilePath}");
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class SummaryCommandHandler : ICommand
    {
        public string Command { get; } = "stc_summary";
        public string[] Aliases { get; } = new string[] { "stafftimecounter_summary" };
        public string Description { get; } = "Generates a summary of Staff members' times.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count == 0)
            {
                response = "Use 'stc_summary admin' or 'stc_summary user'.";
                return false;
            }

            string summaryType = arguments.At(0).ToLower();

            if (summaryType == "admin" || summaryType == "user")
            {
                var generator = new SummaryGenerator();
                generator.OnSummaryCommandWithLogging();
                response = "The summary report was generated successfully.";
                return true;
            }

            response = "Invalid summary type. Use 'admin' or 'user'.";
            return false;
        }
    }
}
