using System.ComponentModel.DataAnnotations;

namespace WebOptimus.Models
{
    public enum ProcedureFrequency
    {
        Hourly,
        Daily,
        Monthly
    }

    public class ScheduledStoredProcedure
    {
        [Key]
        public int Id { get; set; }
        public string ProcedureName { get; set; } = string.Empty;
        public string EmailSubject { get; set; } = string.Empty;
        public string ToEmail { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastRunDate { get; set; }
        public DateTime CreatedOn { get; set; }
        public TimeSpan ScheduledTime { get; set; }
        public ProcedureFrequency Frequency { get; set; } // New field
        public int? DayOfMonth { get; set; }
        public bool IsRunNow { get; set; } // default false

    }

}
