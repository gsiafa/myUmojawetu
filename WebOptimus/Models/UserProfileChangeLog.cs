namespace WebOptimus.Models
{
    public class UserProfileChangeLog
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime ChangeDate { get; set; }
        public string ChangedBy { get; set; }
    }
}
