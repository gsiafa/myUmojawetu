

using WebOptimus.Models;

namespace WebOptimus.Services
{
    public interface IPostmarkClient
    {
        Task<PostmarkStatus> SendMessageAsync(PostmarkMessage message, CancellationToken ct);
        Task<List<(string To, PostmarkStatus Status)>> SendBatchMessagesAsync(List<PostmarkMessage> messages, CancellationToken ct);
    }
}
