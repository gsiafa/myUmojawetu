namespace WebOptimus.Helpers
{
    using System;
    using System.Linq;
    using WebOptimus;
   using Microsoft.Extensions.Primitives;

    public class RequestIpHelper
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public RequestIpHelper(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public string GetRequestIp(bool tryUseXForwardHeader = true)
        {
            string ip = null;
            if (tryUseXForwardHeader)
            {
                ip = GetHeaderValueAs<string>("X-Forwarded-For").SplitCsv().FirstOrDefault();

            }

            if (ip.IsNullOrWhitespace() && httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress != null)
            {
                ip = httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            }

            if (ip.IsNullOrWhitespace())
            {
                ip = GetHeaderValueAs<string>("REMOTE_ADDR");
            }

            if (ip.IsNullOrWhitespace())
            {
                throw new Exception("Unable to determine caller's IP.");
            }

            return ip;
        }

        public T GetHeaderValueAs<T>(string headerName)
        {
            StringValues values = new StringValues();

            if (httpContextAccessor.HttpContext?.Request?.Headers?.TryGetValue(headerName, out values) ?? false)
            {
                string rawValues = values.ToString();   // writes out as Csv when there are multiple.

                if (!rawValues.IsNullOrWhitespace())
                {
                    return (T)Convert.ChangeType(values.ToString(), typeof(T));
                }
            }
            return default(T);
        }
    }
}
