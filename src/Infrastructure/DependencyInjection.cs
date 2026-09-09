using Application.Ports;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // Fixed server version avoids an extra AutoDetect round-trip at startup.
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));
        services.AddDbContext<AppDbContext>(opt => opt.UseMySql(connectionString, serverVersion));
        services.AddScoped<IStudentRepository, EfStudentRepository>();
        services.AddScoped<IProgramRepository, EfProgramRepository>();
        services.AddScoped<IProfessorRepository, EfProfessorRepository>();
        services.AddScoped<ICourseRepository, EfCourseRepository>();
        services.AddScoped<IEnrollmentRepository, EfEnrollmentRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        return services;
    }
}
