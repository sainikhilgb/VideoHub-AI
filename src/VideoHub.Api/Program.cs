using Microsoft.EntityFrameworkCore;
using VideoHub.Api.Application.DependencyInjection;
using VideoHub.Api.Infrastructure.DependencyInjection;
using VideoHub.Api.Infrastructure.Extensions;
using VideoHub.Api.Infrastructure.Logging;
using VideoHub.Api.Infrastructure.Middleware;
using DotNetEnv;
using VideoHub.Api.Infrastructure.Options;


var builder = WebApplication.CreateBuilder(args);
// Load .env variables into the process
Env.Load();

// Merge environment variables into ASP.NET Core configuration
builder.Configuration.AddEnvironmentVariables();

builder.AddStructuredLogging();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        if (context.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
        {
            context.ProblemDetails.Extensions["correlationId"] = correlationId?.ToString();
        }
    };
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<BlobStorageOptions>(
    builder.Configuration.GetSection("BlobStorage"));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseInfrastructure();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogWarning(
        "Application started. Listening on: {Urls}",
        string.Join(", ", app.Urls));
});

app.Lifetime.ApplicationStopped.Register(() =>
{
    app.Logger.LogWarning("Application stopped.");
});

app.Run();
