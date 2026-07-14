namespace VideoHub.Api.Application.Exceptions;

public sealed class InvalidRefreshTokenException : AuthenticationException
{
    public InvalidRefreshTokenException()
        : base("The refresh token is invalid or has been revoked.")
    {
    }

    public InvalidRefreshTokenException(string message)
        : base(message)
    {
    }
}
