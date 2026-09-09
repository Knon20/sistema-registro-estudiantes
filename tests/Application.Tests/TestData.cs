using Domain.Entities;

namespace Application.Tests;

internal static class TestData
{
    public static List<Course> ThreeValidCourses()
    {
        var p1 = Guid.NewGuid(); var p2 = Guid.NewGuid(); var p3 = Guid.NewGuid();
        return new List<Course>
        {
            new("Cálculo I", "MAT-101", p1),
            new("Programación I", "SIS-103", p2),
            new("Inglés Técnico", "HUM-107", p3),
        };
    }
}
