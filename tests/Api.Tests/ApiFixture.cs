using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.MySql;

namespace Api.Tests;

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiFixture> { }

public sealed class ApiFixture : IAsyncLifetime
{
    private readonly MySqlContainer _mysql = new MySqlBuilder()
        .WithImage("mysql:8.0")
        .WithDatabase("StudentDb")
        .WithUsername("root")
        .WithPassword("Test-only1!")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = default!;
    public HttpClient Client => Factory.CreateClient();

    public async Task InitializeAsync()
    {
        await _mysql.StartAsync();
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:Default", _mysql.GetConnectionString()));
        // Force boot (runs migrations + seed) before first test.
        using var client = Factory.CreateClient();
        var health = await client.GetAsync("/health");
        health.EnsureSuccessStatusCode();
    }

    public async Task DisposeAsync()
    {
        Factory.Dispose();
        await _mysql.DisposeAsync();
    }
}

public sealed record StudentDto(
    Guid Id, string FullName, string Email, string DocumentId,
    Guid ProgramId, string? ProgramName, DateTime CreatedAt);

public sealed record Paged<T>(List<T> Items, int Total);

public sealed record CourseDto(
    Guid Id, string Name, string Code, int Credits, Guid ProfessorId, string? ProfessorName);

public sealed record EnrollmentDto(
    Guid Id, Guid StudentId, string StudentName, string Period, int TotalCredits,
    List<CourseDto> Courses, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record ErrorBody(string Code, string Message, object? Data);
