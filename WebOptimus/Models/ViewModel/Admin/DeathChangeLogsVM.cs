namespace WebOptimus.Models.ViewModel.Admin
{
    public class DeathChangeLogsVM
    {
        public int Id { get; set; }
        public Guid? UserId { get; set; }
        public int ReportedDeathId { get; set; }
        public string? Action { get; set; } // Added, Edited, Deleted
        public string? FieldName { get; set; }
        public string? OldValue { get; set; }
        public DateTime ChangeDate { get; set; }
        public string? NewValue { get; set; }
        public DateTime? DateChanged { get; set; }
        public string? ChangedBy { get; set; }

        public int? NextOfKinId { get; set; }
    }
}
