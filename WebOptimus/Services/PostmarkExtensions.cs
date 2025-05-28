namespace WebOptimus
{
    using WebOptimus.Configuration;
    using WebOptimus.Services;
    using System.Net;

    public static class PostmarkExtensions
    {
        public static IServiceCollection AddPostmark(this IServiceCollection services)
        {
            if (WebAppContext.IsRunningInAzureWebApp)
            {
                services.AddHttpClient<IPostmarkClient, PostmarkClient>();
            }
            else
            {
                services.AddHttpClient<IPostmarkClient, PostmarkClient>()
                    .ConfigureHttpMessageHandlerBuilder(_ => new HttpClientHandler()
                    {
                        Proxy = WebRequest.GetSystemWebProxy(),
                        UseProxy = true,
                        Credentials = CredentialCache.DefaultCredentials
                    });
            }

            return services;
        }
    }
}
