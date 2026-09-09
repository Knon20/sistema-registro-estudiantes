using Domain.Entities;
using Domain.ReferenceData;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public static class SeedData
{
    private const string Period = "2026-1";

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

        if (!await db.Students.AnyAsync(ct))
        {
            var program = await db.Programs.FirstAsync(p => p.Code == "IS-01", ct);
            var courses = await db.Courses.ToListAsync(ct);
            var byCode = courses.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

            // 5 demo students, each with a valid enrollment (3 courses, distinct professors).
            // Shared courses across students so "classmates" has data from the start.
            var demos = new (string Name, string Email, string Doc, string[] Codes)[]
            {
                ("Laura Méndez", "laura.mendez@uni.edu", "2001001", new[] { "MAT-101", "SIS-103", "SIS-105" }),
                ("Diego Torres", "diego.torres@uni.edu", "2001002", new[] { "FIS-102", "SIS-104", "SIS-106" }),
                ("Camila Rojas", "camila.rojas@uni.edu", "2001003", new[] { "MAT-101", "HUM-107", "MAT-109" }),
                ("Andrés Peña", "andres.pena@uni.edu", "2001004", new[] { "SIS-103", "HUM-108", "SIS-110" }),
                ("Valentina Cruz", "valentina.cruz@uni.edu", "2001005", new[] { "SIS-105", "HUM-107", "SIS-110" }),
            };

            foreach (var (name, email, doc, codes) in demos)
            {
                var student = new Student(name, email, doc, program.Id);
                await db.Students.AddAsync(student, ct);
                // Enrollment.Create validates domain rules: fails fast if seed is ever invalid.
                var enrollment = Enrollment.Create(
                    student.Id, Period, codes.Select(c => byCode[c]).ToList());
                await db.Enrollments.AddAsync(enrollment, ct);
                foreach (var item in enrollment.Items)
                    await db.EnrollmentCourses.AddAsync(item, ct);
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
