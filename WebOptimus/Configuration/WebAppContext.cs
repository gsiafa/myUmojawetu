namespace WebOptimus.Configuration
{
    public static class WebAppContext
    {
        public static string HomeFolder { get; } = Environment.GetEnvironmentVariable("HOME") ?? string.Empty;

        public static string SiteName { get; } = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? string.Empty;

        public static string SiteInstanceId { get; } = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? string.Empty;

        public static bool IsRunningInAzureWebApp => !string.IsNullOrEmpty(HomeFolder) &&
                                              !string.IsNullOrEmpty(SiteName);
    }
}
