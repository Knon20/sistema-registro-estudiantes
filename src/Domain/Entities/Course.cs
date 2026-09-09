using Domain.Common;

namespace Domain.Entities;

public sealed class Course : Entity
{
    public const int CreditsPerCourse = 3;

    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public int Credits { get; private set; } = CreditsPerCourse;
    public Guid ProfessorId { get; private set; }

    private Course() { }

    public Course(string name, string code, Guid professorId, int credits = CreditsPerCourse)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Course name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Course code is required.", nameof(code));
        if (credits != CreditsPerCourse) throw new ArgumentException($"Course credits must be {CreditsPerCourse}.", nameof(credits));
        if (professorId == Guid.Empty) throw new ArgumentException("Professor is required.", nameof(professorId));
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        ProfessorId = professorId;
        Credits = credits;
    }
}
