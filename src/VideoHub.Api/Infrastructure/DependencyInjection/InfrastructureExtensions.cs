using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Infrastructure.BackgroundJobs;
using VideoHub.Api.Infrastructure.Caching;
using VideoHub.Api.Infrastructure.Persistence;
using VideoHub.Api.Infrastructure.Persistence.Repositories;
using VideoHub.Api.Infrastructure.Options;
using VideoHub.Api.Infrastructure.Storage;

namespace VideoHub.Api.Infrastructure.DependencyInjection;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing database connection string.");

        services.Configure<HangfireSettings>(configuration.GetSection("Hangfire"));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IBlobStorage, LocalBlobStorage>();
        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["Redis:Configuration"];
            options.InstanceName = configuration["Redis:InstanceName"] ?? "VideoHub:";
        });

        services.AddHangfire(config =>
        {
            config.UseSimpleAssemblyNameTypeSerializer();
            config.UseRecommendedSerializerSettings();
            config.UseFilter(new HangfireJobExecutionLoggingFilter());
            config.UsePostgreSqlStorage(
                options => options.UseNpgsqlConnection(connectionString, _ => { }),
                new PostgreSqlStorageOptions());
        });

        services.AddHangfireServer();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("VideoHub.Api"))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                tracing.AddEntityFrameworkCoreInstrumentation();
                tracing.AddOtlpExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
                metrics.AddOtlpExporter();
            });

        return services;
    }

    public static IHostApplicationBuilder AddStructuredLogging(this IHostApplicationBuilder builder)
    {
        var logFilePath = builder.Configuration["Serilog:FilePath"] ?? "logs/videohub-.log";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Warning)
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Information)
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Services.AddSerilog((_, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Information()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Warning)
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Information);
        }, preserveStaticLogger: true, writeToProviders: false);
        return builder;
    }
}
