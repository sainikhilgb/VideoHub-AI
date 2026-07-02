using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using VideoHub.Api.Application.BackgroundJobs;

namespace VideoHub.Api.Application.DependencyInjection;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();
        return services;
    }
}
