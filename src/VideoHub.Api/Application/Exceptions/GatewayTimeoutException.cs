namespace VideoHub.Api.Application.Exceptions;

public sealed class GatewayTimeoutException : Exception
{
    public GatewayTimeoutException(string message)
        : base(message)
    {
    }
}
