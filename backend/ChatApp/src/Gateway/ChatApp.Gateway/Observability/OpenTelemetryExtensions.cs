using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ChatApp.Gateway.Observability
{
    public static class OpenTelemetryExtensions
    {
        public static IServiceCollection AddOpenTelemetryExtensions (this IServiceCollection services, IConfiguration configuration)
        {
            var serviceName = "gateway";
            var resource = ResourceBuilder.CreateDefault().AddService(serviceName);

            services.AddOpenTelemetry()
           .ConfigureResource(r => r.AddService(serviceName))
           .WithMetrics(m => m
               .SetResourceBuilder(resource)
               .AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddRuntimeInstrumentation()
               .AddOtlpExporter())
           .WithTracing(t => t
               .SetResourceBuilder(resource)
               .AddAspNetCoreInstrumentation(o => o.RecordException = true)
               .AddHttpClientInstrumentation()
               .AddOtlpExporter());

            return services;
        }
    }
}
