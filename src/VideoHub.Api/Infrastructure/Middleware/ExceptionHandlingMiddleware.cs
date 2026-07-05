using Microsoft.AspNetCore.Mvc;

namespace VideoHub.Api.Infrastructure.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ExceptionHandlingMiddleware> logger;
    private readonly IProblemDetailsService problemDetailsService;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IProblemDetailsService problemDetailsService)
    {
        this.next = next;
        this.logger = logger;
        this.problemDetailsService = problemDetailsService;
    }

   public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await next(context);
    }
    catch (FluentValidation.ValidationException ex)
    {
        logger.LogWarning(ex, "Validation failed.");

        await WriteProblemDetailsAsync(
            context,
            StatusCodes.Status400BadRequest,
            "Validation Failed",
            ex.Message);
    }
    catch (BadRequestException ex)
    {
        logger.LogWarning(ex, "Bad request.");

        await WriteProblemDetailsAsync(
            context,
            StatusCodes.Status400BadRequest,
            "Bad Request",
            ex.Message);
    }
    catch (UnauthorizedException ex)
    {
        logger.LogWarning(ex, "Unauthorized request.");

        await WriteProblemDetailsAsync(
            context,
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            ex.Message);
    }
    catch (ForbiddenException ex)
    {
        logger.LogWarning(ex, "Forbidden request.");

        await WriteProblemDetailsAsync(
            context,
            StatusCodes.Status403Forbidden,
            "Forbidden",
            ex.Message);
    }
    catch (NotFoundException ex)
    {
        logger.LogWarning(ex, "Resource not found.");

        await WriteProblemDetailsAsync(
            context,
            StatusCodes.Status404NotFound,
            "Resource Not Found",
            ex.Message);
    }
    catch (ConflictException ex)
    {
        logger.LogWarning(ex, "Conflict occurred.");

        await WriteProblemDetailsAsync(
            context,
            StatusCodes.Status409Conflict,
            "Conflict",
            ex.Message);
    }
    catch (ServiceUnavailableException ex)
    {
        logger.LogError(ex, "Service unavailable.");

        await WriteProblemDetailsAsync(
            context,
            StatusCodes.Status503ServiceUnavailable,
            "Service Unavailable",
            ex.Message);
    }
    catch (GatewayTimeoutException ex)
    {
        logger.LogError(ex, "Gateway timeout.");

        await WriteProblemDetailsAsync(
            context,
            StatusCodes.Status504GatewayTimeout,
            "Gateway Timeout",
            ex.Message);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An unhandled exception occurred.");

        var hostEnvironment = context.RequestServices.GetRequiredService<IHostEnvironment>();

        await WriteProblemDetailsAsync(
            context,
            StatusCodes.Status500InternalServerError,
            "Internal Server Error",
            hostEnvironment.IsDevelopment()
                ? ex.Message
                : "An unexpected error occurred.");
    }
}

private async Task WriteProblemDetailsAsync(
    HttpContext context,
    int statusCode,
    string title,
    string detail)
{
    context.Response.StatusCode = statusCode;

    var problemDetails = new ProblemDetails
    {
        Status = statusCode,
        Title = title,
        Detail = detail,
        Instance = context.Request.Path
    };

    problemDetails.Extensions["traceId"] = context.TraceIdentifier;

    await problemDetailsService.WriteAsync(new ProblemDetailsContext
    {
        HttpContext = context,
        ProblemDetails = problemDetails
    });
}
    public sealed class NotFoundException : Exception
    {
        public NotFoundException(string message)
            : base(message)
        {
        }
    }
    public sealed class BadRequestException : Exception
    {
        public BadRequestException(string message)
            : base(message)
        {
        }
    }
    public sealed class ValidationException : Exception
    {
        public ValidationException(string message)
            : base(message)
        {
        }
    }
    public sealed class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message)
            : base(message)
        {
        }
    }
    public sealed class ForbiddenException : Exception
    {
        public ForbiddenException(string message)
            : base(message)
        {
        }
    }
    public sealed class ConflictException : Exception
    {
        public ConflictException(string message)
            : base(message)
        {
        }
    }
    public sealed class InternalServerErrorException : Exception
    {
        public InternalServerErrorException(string message)
            : base(message)
        {
        }
    }
    public sealed class ServiceUnavailableException : Exception
    {
        public ServiceUnavailableException(string message)
            : base(message)
        {
        }
    }
    public sealed class GatewayTimeoutException : Exception
    {
        public GatewayTimeoutException(string message)
            : base(message)
        {
        }
    }
}