namespace WebOptimus.Models
{
    public class ScheduledTask
    {
        public int Id { get; set; }

        public TimeSpan ExecutionTime { get; set; } //HH:mm:ss format

        public bool IsActive { get; set; }
    }
}
