using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace VideoHub.Api.Infrastructure.Authentication;

public static class AiCallbackSecretResolver
{
    public const string DefaultDevelopmentSecret = "VideoHubAI_Secure_Callback_Secret_2026";

    /// <summary>
    /// Resolves the callback secret configured for AI service communication.
    /// In Development mode, falls back to a default secret if unconfigured.
    /// In Production mode, fails closed if the secret configuration is missing or blank.
    /// </summary>
    public static string? ResolveSecret(IConfiguration configuration, IHostEnvironment environment)
    {
        var secret = configuration["AiService:CallbackSecret"];
        if (!string.IsNullOrWhiteSpace(secret))
        {
            return secret;
        }

        if (environment.IsDevelopment())
        {
            return DefaultDevelopmentSecret;
        }

        return null;
    }
}
