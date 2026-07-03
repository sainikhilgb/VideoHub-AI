using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using VideoHub.Api.Application.BackgroundJobs;
using VideoHub.Api.Application.Uploads;

namespace VideoHub.Api.Application.DependencyInjection;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();
        services.AddScoped<IMediaUploadService, MediaUploadService>();
        return services;
    }
}
