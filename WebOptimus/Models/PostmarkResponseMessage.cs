namespace WebOptimus.Models
{
    public class PostmarkResponseMessage
    {
        public string To { get; set; } = string.Empty;
        public string SubmittedAt { get; set; } = string.Empty;
        public string MessageID { get; set; } = string.Empty;
        public int ErrorCode { get; set; }  // 0 means success, anything else means failure
        public string Message { get; set; } = string.Empty;
    }
}

