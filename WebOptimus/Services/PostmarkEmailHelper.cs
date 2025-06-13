using WebOptimus.Models;

namespace WebOptimus.Services
{
    public class PostmarkEmailHelper
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IPostmarkClient _postmarkClient;

        public PostmarkEmailHelper(IServiceScopeFactory scopeFactory, IPostmarkClient postmarkClient)
        {
            _scopeFactory = scopeFactory;
            _postmarkClient = postmarkClient;
        }

        public async Task SendTemplateEmailToUsersAsync(
            string templatePath,
            Dictionary<string, string> replacements,
            string subject,
            string context,
            CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var users = await userService.GetAllActiveUsersAsync(ct);
            if (!users.Any()) return;

            string htmlBody;
            using (var reader = File.OpenText(templatePath))
            {
                htmlBody = await reader.ReadToEndAsync();
            }

            foreach (var pair in replacements)
            {
                htmlBody = htmlBody.Replace(pair.Key, pair.Value);
            }

            var messages = users.Select(user => new PostmarkMessage
            {
                From = "info@umojawetu.com",
                To = user.Email,
                Subject = subject,
                HtmlBody = htmlBody,
                MessageStream = "outbound"
            }).ToList();

            var results = await _postmarkClient.SendBatchMessagesAsync(messages, ct);

            // Optional logging
            foreach (var result in results)
            {
                Console.WriteLine($"To: {result.To}, Status: {result.Status}");
            }
        }
    }

}
