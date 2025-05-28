namespace WebOptimus.Models
{
    public class MissedPaymentsEmailSettings
    {
        public int Id { get; set; }
        public bool RunEveryDay { get; set; }
        public int IntervalValue { get; set; }
    }
}
