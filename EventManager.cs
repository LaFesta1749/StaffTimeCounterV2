// EventManager.cs - Отговорен за регистрацията на събития
using PluginAPI.Events;
using PluginAPI.Core;
using System;

namespace StaffTimeCounterV2
{
    public static class EventManager
    {
        public static void RegisterEvents()
        {
            // Register events here such as player joining or disconnecting, etc.
            Log.Info("Registering events for StaffTimeCounterV2...");
        }
    }
}