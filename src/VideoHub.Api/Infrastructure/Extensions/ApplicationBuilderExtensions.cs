using Hangfire;
using Microsoft.Extensions.Options;
using Serilog;
using VideoHub.Api.Infrastructure.Options;
using VideoHub.Api.Infrastructure.Middleware;

namespace VideoHub.Api.Infrastructure.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseStatusCodePages();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        var hangfireSettings = app.Services.GetRequiredService<IOptions<HangfireSettings>>().Value;
        app.MapHangfireDashboard(hangfireSettings.DashboardPath);
        return app;
    }
}
