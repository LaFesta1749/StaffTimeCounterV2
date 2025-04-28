namespace StaffTimeCounterV2.Models
{
    public class DailyRecord
    {
        public string Name { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string RankName { get; set; } = string.Empty;
        public int ServerTime { get; set; } = 0;
        public int OverwatchTime { get; set; } = 0;
    }
}
