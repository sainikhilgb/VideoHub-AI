namespace VideoHub.Api.Application.Exceptions;

public sealed class TokenExpiredException : AuthenticationException
{
    public TokenExpiredException()
        : base("The token has expired.")
    {
    }

    public TokenExpiredException(string message)
        : base(message)
    {
    }
}
