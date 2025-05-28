namespace WebOptimus.Models
{
    public class DependentChangeLog
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }

        public string? Type { get; set; } = string.Empty;
        public string? Reason { get; set; } = string.Empty;
        public int DependentId { get;set; }
        public string? FieldName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; } = string.Empty;
        public DateTime ChangeDate { get; set; }
        public string ChangedBy { get; set; }
    }
}
