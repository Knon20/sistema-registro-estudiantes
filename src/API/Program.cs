using API.Middleware;
using Application;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Student Registration API", Version = "v1" });
});

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Server=mysql;Database=StudentDb;User=root;Password=Str0ng_Passw0rd!;";

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("mysql");

builder.Services.AddCors(opt => opt.AddPolicy("frontend", p =>
    p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("frontend");
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Auto-migrate + seed (simple bootstrap for the technical test).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var retries = 0;
    while (true)
    {
        try
        {
            await db.Database.MigrateAsync();
            await SeedData.EnsureSeededAsync(db);
            break;
        }
        catch (Exception ex) when (retries < 10)
        {
            retries++;
            logger.LogWarning(ex, "DB not ready, retry {Retry}/10...", retries);
            await Task.Delay(3000);
        }
    }
}

app.Run();

public partial class Program { }
