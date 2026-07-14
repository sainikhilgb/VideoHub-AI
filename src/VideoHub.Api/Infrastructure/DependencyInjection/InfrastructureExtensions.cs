using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using System.Text;
using VideoHub.Api.Application.Authentication;
using VideoHub.Api.Application.CurrentUser;
using VideoHub.Api.Application.Users;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Infrastructure.Authentication;
using VideoHub.Api.Infrastructure.Authentication.Jwt;
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
        services.Configure<BlobStorageOptions>(configuration.GetSection(BlobStorageOptions.SectionName));
        services.Configure<AiServiceOptions>(configuration.GetSection(AiServiceOptions.SectionName));

        // Configure JWT options & authentication
        var jwtSettings = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(jwtSettings);

        var jwtOptions = jwtSettings.Get<JwtOptions>()
            ?? throw new InvalidOperationException("Missing Jwt configuration section.");

        if (string.IsNullOrEmpty(jwtOptions.Secret) || jwtOptions.Secret.Length < 16)
        {
            throw new InvalidOperationException("Jwt Secret must be configured and be at least 16 characters long.");
        }

        var secretKey = Encoding.UTF8.GetBytes(jwtOptions.Secret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds)
            };
        });

        services.AddHttpClient("AiService", (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<AiServiceOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProjectRepository, ProjectRepositoryService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IMediaStoragePathBuilder, MediaStoragePathBuilder>();
        services.AddHttpClient<SupabaseBlobStorage>();
        services.AddScoped<LocalBlobStorage>();
        services.AddScoped<IBlobStorage>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
            return string.Equals(options.Provider, "Supabase", StringComparison.OrdinalIgnoreCase)
                ? serviceProvider.GetRequiredService<SupabaseBlobStorage>()
                : serviceProvider.GetRequiredService<LocalBlobStorage>();
        });

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

        services.AddHttpContextAccessor();
        return services;
    }

    public static IHostApplicationBuilder AddStructuredLogging(this IHostApplicationBuilder builder)
    {
        var logFilePath = builder.Configuration["Serilog:FilePath"] ?? "logs/videohub-.log";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", "VideoHub.Api")
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
                .Enrich.WithProperty("ServiceName", "VideoHub.Api")
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Warning)
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Information);
        }, preserveStaticLogger: true, writeToProviders: false);
        return builder;
    }
}
