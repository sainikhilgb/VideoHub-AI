using Hangfire;
using Microsoft.Extensions.Options;
using Serilog;
using VideoHub.Api.Infrastructure.Options;
using VideoHub.Api.Infrastructure.Middleware;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

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

        var storageOptions = app.Services.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
        var rootPath = string.IsNullOrWhiteSpace(storageOptions.LocalPath)
            ? Path.Combine(AppContext.BaseDirectory, "blobs")
            : storageOptions.LocalPath;
        Directory.CreateDirectory(rootPath);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(rootPath),
            RequestPath = "/blobs"
        });

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        var hangfireSettings = app.Services.GetRequiredService<IOptions<HangfireSettings>>().Value;
        app.MapHangfireDashboard(hangfireSettings.DashboardPath);
        return app;
    }
}
