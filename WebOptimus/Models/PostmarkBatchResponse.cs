namespace WebOptimus.Models
{
    public class PostmarkBatchResponse
    {
        public List<PostmarkMessageResponse> Messages { get; set; }
    }

    public class PostmarkMessageResponse
    {
        public string To { get; set; }
        public int ErrorCode { get; set; }
        public string Message { get; set; }
    }

}
