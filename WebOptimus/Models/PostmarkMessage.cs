namespace WebOptimus.Models
{
    public class PostmarkMessage
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string Cc { get; set; } = string.Empty;
        public string Bcc { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public string TextBody { get; set; } = string.Empty;
        public string ReplyTo { get; set; } = string.Empty;
        public bool TrackOpens { get; set; } = false;
        public string TrackLinks { get; set; } = string.Empty;
        public string MessageStream { get; set; } = "outbound";

    }
}
