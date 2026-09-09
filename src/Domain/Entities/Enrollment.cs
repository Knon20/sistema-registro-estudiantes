using Domain.Common;
using Domain.Entities;
using Domain.Exceptions;

namespace Domain.Entities;

/// <summary>
/// Enrollment is the aggregate root that protects all invariants:
/// exactly 3 distinct courses, 9 total credits, distinct professors.
/// </summary>
public sealed class Enrollment : Entity
{
    public const int RequiredCourses = 3;
    public const int RequiredCredits = 9;

    public Guid StudentId { get; private set; }
    public string Period { get; private set; } = "2026-1";
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public bool IsActive => DeletedAt is null;

    private readonly List<EnrollmentCourse> _items = new();
    public IReadOnlyCollection<EnrollmentCourse> Items => _items.AsReadOnly();

    /// <summary>EF Core / serializers.</summary>
    private Enrollment() { }

    private Enrollment(Guid studentId, string period)
    {
        if (studentId == Guid.Empty) throw new ArgumentException("Student is required.", nameof(studentId));
        StudentId = studentId;
        Period = string.IsNullOrWhiteSpace(period) ? "2026-1" : period.Trim();
    }

    public static Enrollment Create(Guid studentId, string period, IReadOnlyList<Course> selectedCourses)
    {
        var enrollment = new Enrollment(studentId, period);
        enrollment.ReplaceCourses(selectedCourses);
        return enrollment;
    }

    public void ReplaceCourses(IReadOnlyList<Course> selectedCourses)
    {
        EnrollmentRules.ValidateSelection(selectedCourses);
        _items.Clear();
        foreach (var c in selectedCourses)
            _items.Add(new EnrollmentCourse(Id, c.Id));
        UpdatedAt = DateTime.UtcNow;
    }

    public IReadOnlyList<Guid> CourseIds() => _items.Select(i => i.CourseId).ToList();

    /// <summary>Soft delete: DeletedAt set = hidden by global query filter. Audit preserved.</summary>
    public void MarkDeleted()
    {
        DeletedAt ??= DateTime.UtcNow;
    }

    public int TotalCredits(IReadOnlyList<Course> selectedCourses) => selectedCourses.Sum(c => c.Credits);
}

public sealed class EnrollmentCourse
{
    public Guid EnrollmentId { get; private set; }
    public Guid CourseId { get; private set; }

    private EnrollmentCourse() { }

    public EnrollmentCourse(Guid enrollmentId, Guid courseId)
    {
        if (enrollmentId == Guid.Empty) throw new ArgumentException("Enrollment is required.", nameof(enrollmentId));
        if (courseId == Guid.Empty) throw new ArgumentException("Course is required.", nameof(courseId));
        EnrollmentId = enrollmentId;
        CourseId = courseId;
    }
}
