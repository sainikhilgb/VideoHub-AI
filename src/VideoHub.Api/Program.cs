using VideoHub.Api.Application.DependencyInjection;
using VideoHub.Api.Infrastructure.DependencyInjection;
using VideoHub.Api.Infrastructure.Extensions;
using VideoHub.Api.Infrastructure.Middleware;
using DotNetEnv;


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
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<VideoHub.Api.Infrastructure.Persistence.AppDbContext>();
    var defaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000000");
    if (!dbContext.Users.Any(u => u.Id == defaultUserId))
    {
        dbContext.Users.Add(new VideoHub.Api.Domain.Entities.User
        {
            Id = defaultUserId,
            Email = "system_default@example.com",
            Role = "User",
            DisplayName = "Default Local User",
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.SaveChanges();
    }
}

app.Run();
