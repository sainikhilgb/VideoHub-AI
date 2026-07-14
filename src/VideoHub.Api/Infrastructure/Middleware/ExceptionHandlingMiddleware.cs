using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoHub.Api.Application.Exceptions;

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
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is Npgsql.PostgresException pgEx)
            {
                if (pgEx.SqlState == "23503") // Foreign Key Violation
                {
                    logger.LogWarning(ex, "Database foreign key constraint violation.");
                    await WriteProblemDetailsAsync(
                        context,
                        StatusCodes.Status400BadRequest,
                        "Bad Request",
                        "The request violates a database foreign key constraint. The referenced entity does not exist.");
                    return;
                }
                if (pgEx.SqlState == "23505") // Unique Violation
                {
                    logger.LogWarning(ex, "Database unique constraint violation.");
                    await WriteProblemDetailsAsync(
                        context,
                        StatusCodes.Status409Conflict,
                        "Conflict",
                        "The entity violates a database unique constraint.");
                    return;
                }
            }

            logger.LogError(ex, "A database update exception occurred.");
            var hostEnvironment = context.RequestServices.GetRequiredService<IHostEnvironment>();
            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                hostEnvironment.IsDevelopment()
                    ? ex.Message
                    : "An unexpected database error occurred.");
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning("Request was cancelled.");
            context.Response.StatusCode = 499; // Client Closed Request
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
        catch (AuthenticationException ex)
        {
            logger.LogWarning(ex, "Authentication failed.");

            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Authentication Failed",
                "Authentication failed.");
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
        catch (KeyNotFoundException ex)
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
        if (context.Response.HasStarted)
        {
            logger.LogWarning("Response has already started. Cannot write ProblemDetails for {StatusCode}.", statusCode);
            return;
        }

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
}