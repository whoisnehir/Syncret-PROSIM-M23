namespace SyncretAPI.Models
{
    public class ProcessLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Component { get; set; } = "";
        public string EventType { get; set; } = "";
        public string? Message { get; set; }
    }
}