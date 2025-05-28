using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using WebOptimus.Data;

namespace WebOptimus.Configuration
{


    public static class HealthCheckExtensions
    {
        public static WebApplicationBuilder AddHealthChecks(this WebApplicationBuilder builder)
        {
            builder.Services.AddHealthChecks()
               ;

            return builder;
        }


        public static WebApplication UseAppHealthChecks(this WebApplication app)
        {
            app.MapHealthChecks("/health/startup");
            app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false });
            app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = _ => false });

            return app;
        }
    }
}
