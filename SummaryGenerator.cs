using Exiled.API.Features;
using CommandSystem;
using StaffTimeCounterV2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace StaffTimeCounterV2
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class SummaryGenerator : ICommand
    {
        public string Command => "stc_summary";
        public string[] Aliases => new string[0];
        public string Description => "Generates a summary of staff members' playtime.";

        private readonly string timesDirectory = Path.Combine(Paths.Configs, "StaffTimeCounterV2", "Data", "Times");
        private readonly string summariesDirectory = Path.Combine(Paths.Configs, "StaffTimeCounterV2", "Data", "Summaries");

        public SummaryGenerator()
        {
            EnsureDirectoriesExist();
        }

        private void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(timesDirectory))
                Directory.CreateDirectory(timesDirectory);

            if (!Directory.Exists(summariesDirectory))
                Directory.CreateDirectory(summariesDirectory);
        }

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count == 0)
            {
                response = "Usage: stc_summary Admin";
                return false;
            }

            string mode = arguments.At(0).ToLower();

            if (mode != "admin")
            {
                response = "Invalid argument. Use: stc_summary Admin";
                return false;
            }

            try
            {
                var files = Directory.GetFiles(timesDirectory, "StaffTimeCounter_Day_*.yml");

                if (files.Length == 0)
                {
                    response = "No daily files found to generate a summary.";
                    return false;
                }

                List<DailyRecord> combinedRecords = new();
                DateTime? startDate = null;
                DateTime? endDate = null;
                DateTime today = DateTime.Today;

                foreach (var filePath in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string datePart = fileName.Replace("StaffTimeCounter_Day_", "");

                    if (!DateTime.TryParseExact(datePart, "dd_MM_yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fileDate))
                    {
                        Log.Debug($"Invalid date format in file: {filePath}");
                        continue;
                    }

                    if (fileDate == today)
                        continue;

                    if (startDate == null || fileDate < startDate) startDate = fileDate;
                    if (endDate == null || fileDate > endDate) endDate = fileDate;

                    var deserializer = new DeserializerBuilder().Build();
                    using var reader = new StreamReader(filePath);

                    var dailyRecords = deserializer.Deserialize<List<DailyRecord>>(reader) ?? new List<DailyRecord>();

                    foreach (var record in dailyRecords)
                    {
                        var existing = combinedRecords.FirstOrDefault(x => x.UserId == record.UserId);
                        if (existing != null)
                        {
                            existing.ServerTime += record.ServerTime;
                            existing.OverwatchTime += record.OverwatchTime;
                        }
                        else
                        {
                            combinedRecords.Add(record);
                        }
                    }

                    File.Delete(filePath);
                }

                if (startDate != null && endDate != null)
                {
                    SaveSummary(startDate.Value, endDate.Value, combinedRecords);
                    response = $"Summary generated successfully from {startDate.Value:dd.MM.yyyy} to {endDate.Value:dd.MM.yyyy}.";
                    return true;
                }
                else
                {
                    response = "No valid data found to generate a summary.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"Summary generation error: {ex}");
                response = $"Error generating summary: {ex.Message}";
                return false;
            }
        }

        private void SaveSummary(DateTime start, DateTime end, List<DailyRecord> records)
        {
            try
            {
                string fileName = $"{start:dd_MM_yyyy} --- {end:dd_MM_yyyy}.yml";
                string filePath = Path.Combine(summariesDirectory, fileName);

                var serializer = new SerializerBuilder().Build();
                using var writer = new StreamWriter(filePath);

                var formatted = records.Select(record => new
                {
                    record.Name,
                    record.UserId,
                    record.RankName,
                    ServerTime = $"\"{record.ServerTime / 60:D2}:{record.ServerTime % 60:D2}\"",
                    OverwatchTime = $"\"{record.OverwatchTime / 60:D2}:{record.OverwatchTime % 60:D2}\""
                }).ToList();

                serializer.Serialize(writer, formatted);

                Log.Debug($"Summary saved successfully at: {filePath}");
            }
            catch (Exception ex)
            {
                Log.Debug($"Failed to save summary file: {ex.Message}");
            }
        }
    }
}
