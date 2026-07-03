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
        catch (FluentValidation.ValidationException exception)
        {
            logger.LogWarning(exception, "Validation failed");
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Validation failed.", exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            logger.LogWarning(exception, "Resource not found");
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Resource not found.", exception.Message);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Storage upload failed");
            await WriteProblemAsync(context, StatusCodes.Status502BadGateway, "Storage upload failed.", exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception occurred");
            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                appEnvironmentMessage(context));
        }
    }

    private static string? appEnvironmentMessage(HttpContext context) =>
        context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
            ? "See logs for details."
            : null;

    private async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string? detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problemDetails
        });
    }
}
