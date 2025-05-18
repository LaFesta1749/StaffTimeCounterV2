using Exiled.API.Features;
using Exiled.Events.Handlers;
using Exiled.Loader;
using System;

namespace StaffTimeCounterV2
{
    public class Plugin : Plugin<Config>
    {
        public static Plugin Instance { get; private set; } = null!;

        public override string Name => "StaffTimeCounterV2";
        public override string Author => "LaFesta1749";
        public override string Prefix => "stcv2";
        public override Version Version => new(1, 1, 1);
        public override Version RequiredExiledVersion => new(9, 6, 0);

        public ConfigManager ConfigManager { get; private set; } = null!;
        public TimeTracker TimeTracker { get; private set; } = null!;
        public SummaryGenerator SummaryGenerator { get; private set; } = null!;

        public override void OnEnabled()
        {
            Instance = this;
            Log.Debug($"{Name} is loading...");

            ConfigManager = new();
            ConfigManager.Load();

            TimeTracker = new();
            TimeTracker.Register();

            SummaryGenerator = new SummaryGenerator();

            base.OnEnabled();
            Log.Debug($"{Name} has loaded successfully.");
        }

        public override void OnDisabled()
        {
            TimeTracker.Unregister();
            ConfigManager = null!;
            TimeTracker = null!;
            SummaryGenerator = null!;
            Instance = null!;

            base.OnDisabled();
            Log.Debug($"{Name} has been disabled.");
        }
    }
}
