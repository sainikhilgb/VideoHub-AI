using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace VideoHub.Api.Infrastructure.Logging;

public static class LoggingExtensions
{
    public static IServiceCollection AddInfrastructureLogging(this IServiceCollection services)
    {
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });

        return services;
    }
}
