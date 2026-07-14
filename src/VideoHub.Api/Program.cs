using VideoHub.Api.Application.DependencyInjection;
using VideoHub.Api.Infrastructure.DependencyInjection;
using VideoHub.Api.Infrastructure.Extensions;
using VideoHub.Api.Infrastructure.Middleware;
using DotNetEnv;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;


var builder = WebApplication.CreateBuilder(args);
// Load .env variables into the process
Env.Load();

// Merge environment variables into ASP.NET Core configuration
builder.Configuration.AddEnvironmentVariables();

builder.AddStructuredLogging();

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            var allowedOrigins = builder.Configuration.GetValue<string>("Cors:AllowedOrigins")
                ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? new[] { "http://localhost:5173" };

            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "VideoHub AI API", Version = "v1" });
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.OperationFilter<AuthorizeCheckOperationFilter>();
});
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

app.UseCors("CorsPolicy");
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

public sealed class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize = context.MethodInfo.DeclaringType != null && (
            context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ||
            context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any()
        );

        var hasAllowAnonymous = context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any();

        if (hasAuthorize && !hasAllowAnonymous)
        {
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

            var oAuthScheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };

            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new() { [oAuthScheme] = Array.Empty<string>() }
            };
        }
    }
}
