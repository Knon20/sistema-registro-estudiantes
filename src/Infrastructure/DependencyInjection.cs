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
        services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionString));
        services.AddScoped<IStudentRepository, EfStudentRepository>();
        services.AddScoped<IProgramRepository, EfProgramRepository>();
        services.AddScoped<IProfessorRepository, EfProfessorRepository>();
        services.AddScoped<ICourseRepository, EfCourseRepository>();
        services.AddScoped<IEnrollmentRepository, EfEnrollmentRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        return services;
    }
}
