using Microsoft.EntityFrameworkCore;
using VideoHub.Api.Infrastructure.Configuration;
using VideoHub.Api.Application.DependencyInjection;
using VideoHub.Api.Infrastructure.DependencyInjection;
using VideoHub.Api.Infrastructure.Extensions;
using VideoHub.Api.Infrastructure.Logging;
using VideoHub.Api.Infrastructure.Middleware;

EnvFileLoader.Load(AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);

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
