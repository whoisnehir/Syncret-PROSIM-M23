namespace SyncretAPI.Models
{
    public class ProcessState
    {
        public bool M1 { get; set; }
        public bool M2 { get; set; }
        public bool M3 { get; set; }
        public bool M4 { get; set; }
        public bool IsAlarm { get; set; }
        public string ClapetaPos { get; set; } = "None";
        public DateTime UpdatedAt { get; set; }
    }
}