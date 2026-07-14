using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VideoHub.Api.Application.Authentication;
using VideoHub.Api.Domain.Entities;

namespace VideoHub.Api.Infrastructure.Authentication.Jwt;

public sealed class JwtService : IJwtService
{
    private readonly JwtOptions options;

    public JwtService(IOptions<JwtOptions> options)
    {
        this.options = options.Value;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        if (!string.IsNullOrEmpty(user.DisplayName))
        {
            claims.Add(new Claim("name", user.DisplayName));
        }

        var signingCredentials = GetSigningCredentials();

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(options.ExpiryMinutes),
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = GetTokenValidationParametersForVerification();
        tokenValidationParameters.ValidateLifetime = false; // We want to read claims of an expired token

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token algorithm");
        }

        return principal;
    }

    // Extensibility point: Future asymmetric signing keys can be plugged in here
    private SigningCredentials GetSigningCredentials()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret));
        return new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    private TokenValidationParameters GetTokenValidationParametersForVerification()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret));
        return new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromSeconds(options.ClockSkewSeconds)
        };
    }
}
