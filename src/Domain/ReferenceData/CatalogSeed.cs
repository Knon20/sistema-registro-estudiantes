namespace Domain.ReferenceData;

/// <summary>
/// Single source of truth for reference catalog data.
/// Used by Infrastructure seed and mirrored in database/seed.sql
/// (see header of that file). Covered by CatalogSeedTests.
/// </summary>
public sealed record ProfessorSeed(string FullName, string Email);
public sealed record CourseSeed(string Name, string Code, string ProfessorEmail);
public sealed record ProgramSeed(string Name, string Code);

public static class CatalogSeed
{
    public static readonly IReadOnlyList<ProgramSeed> Programs = new List<ProgramSeed>
    {
        new("Ingeniería de Sistemas", "IS-01"),
        new("Administración de Empresas", "AD-02"),
    };

    public static readonly IReadOnlyList<ProfessorSeed> Professors = new List<ProfessorSeed>
    {
        new("Ana María Torres", "ana.torres@uni.edu"),
        new("Carlos Ruiz", "carlos.ruiz@uni.edu"),
        new("Jorge Ramírez", "jorge.ramirez@uni.edu"),
        new("Lucía Fernández", "lucia.fernandez@uni.edu"),
        new("Sofía Herrera", "sofia.herrera@uni.edu"),
    };

    // Exactly 2 courses per professor, all worth 3 credits.
    public static readonly IReadOnlyList<CourseSeed> Courses = new List<CourseSeed>
    {
        new("Cálculo I", "MAT-101", "ana.torres@uni.edu"),
        new("Física I", "FIS-102", "ana.torres@uni.edu"),
        new("Programación I", "SIS-103", "carlos.ruiz@uni.edu"),
        new("Bases de Datos", "SIS-104", "carlos.ruiz@uni.edu"),
        new("Redes I", "SIS-105", "jorge.ramirez@uni.edu"),
        new("Sistemas Operativos", "SIS-106", "jorge.ramirez@uni.edu"),
        new("Inglés Técnico", "HUM-107", "lucia.fernandez@uni.edu"),
        new("Ética Profesional", "HUM-108", "lucia.fernandez@uni.edu"),
        new("Estadística", "MAT-109", "sofia.herrera@uni.edu"),
        new("Algoritmos", "SIS-110", "sofia.herrera@uni.edu"),
    };
}
