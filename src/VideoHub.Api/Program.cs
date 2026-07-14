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
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "VideoHub AI API", Version = "v1" });
    
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
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
