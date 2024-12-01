// StaffTimeCounterV2.cs - Главен файл на плъгина
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;

namespace StaffTimeCounterV2
{
    public class StaffTimeCounterV2
    {
        [PluginEntryPoint("StaffTimeCounterV2", "1.0.0", "Plugin that tracks time spent by staff members.", "LaFesta1749")]
        public void OnEnabled()
        {
            PluginAPI.Core.Log.Info("StaffTimeCounterV2 plugin loaded.");
            EventManager.RegisterEvents();
            ConfigManager.LoadConfig();
        }

        [PluginUnload]
        public void OnDisabled()
        {
            PluginAPI.Core.Log.Info("StaffTimeCounterV2 plugin unloaded.");
        }
    }
}