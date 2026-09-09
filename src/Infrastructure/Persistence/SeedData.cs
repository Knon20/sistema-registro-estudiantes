using Domain.Entities;
using Domain.ReferenceData;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public static class SeedData
{
    public static async Task EnsureSeededAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (!await db.Programs.AnyAsync(ct))
        {
            foreach (var p in CatalogSeed.Programs)
                db.Programs.Add(new AcademicProgram(p.Name, p.Code));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Professors.AnyAsync(ct))
        {
            foreach (var p in CatalogSeed.Professors)
                db.Professors.Add(new Professor(p.FullName, p.Email));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Courses.AnyAsync(ct))
        {
            var professors = await db.Professors.ToListAsync(ct);
            var byEmail = professors.ToDictionary(p => p.Email, StringComparer.OrdinalIgnoreCase);
            foreach (var c in CatalogSeed.Courses)
                db.Courses.Add(new Course(c.Name, c.Code, byEmail[c.ProfessorEmail].Id));
            await db.SaveChangesAsync(ct);
        }
    }
}
