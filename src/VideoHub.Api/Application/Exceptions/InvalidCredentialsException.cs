namespace VideoHub.Api.Application.Exceptions;

public sealed class InvalidCredentialsException : AuthenticationException
{
    public InvalidCredentialsException()
        : base("Invalid email or password.")
    {
    }

    public InvalidCredentialsException(string message)
        : base(message)
    {
    }
}
