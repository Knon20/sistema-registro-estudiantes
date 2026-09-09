using Microsoft.Extensions.DependencyInjection;
using Application.UseCases;

namespace Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<StudentService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<EnrollmentService>();
        return services;
    }
}
