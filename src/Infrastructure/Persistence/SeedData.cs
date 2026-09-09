using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public static class SeedData
{
    public static async Task EnsureSeededAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (!await db.Programs.AnyAsync(ct))
        {
            db.Programs.Add(new AcademicProgram("Ingeniería de Sistemas", "IS-01"));
            db.Programs.Add(new AcademicProgram("Administración de Empresas", "AD-02"));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Professors.AnyAsync(ct))
        {
            var names = new[]
            {
                ("Ana María Torres", "ana.torres@uni.edu"),
                ("Carlos Ruiz", "carlos.ruiz@uni.edu"),
                ("Lucía Fernández", "lucia.fernandez@uni.edu"),
                ("Jorge Ramírez", "jorge.ramirez@uni.edu"),
                ("Sofía Herrera", "sofia.herrera@uni.edu"),
            };
            foreach (var (n, e) in names) db.Professors.Add(new Professor(n, e));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Courses.AnyAsync(ct))
        {
            var professors = await db.Professors.OrderBy(p => p.FullName).ToListAsync(ct);
            var courses = new[]
            {
                ("Cálculo I", "MAT-101"), ("Física I", "FIS-102"),
                ("Programación I", "SIS-103"), ("Bases de Datos", "SIS-104"),
                ("Redes I", "SIS-105"), ("Sistemas Operativos", "SIS-106"),
                ("Inglés Técnico", "HUM-107"), ("Ética Profesional", "HUM-108"),
                ("Estadística", "MAT-109"), ("Algoritmos", "SIS-110"),
            };
            for (int i = 0; i < courses.Length; i++)
            {
                var prof = professors[(i / 2) % professors.Count];
                db.Courses.Add(new Course(courses[i].Item1, courses[i].Item2, prof.Id));
            }
            await db.SaveChangesAsync(ct);
        }
    }
}
