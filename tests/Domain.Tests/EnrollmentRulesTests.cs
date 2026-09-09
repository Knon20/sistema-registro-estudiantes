using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;

namespace Domain.Tests;

public sealed class EnrollmentRulesTests
{
    private static List<Course> ThreeValid()
    {
        var p1 = Guid.NewGuid(); var p2 = Guid.NewGuid(); var p3 = Guid.NewGuid();
        return new List<Course>
        {
            new("Cálculo I", "MAT-101", p1),
            new("Programación I", "SIS-103", p2),
            new("Inglés Técnico", "HUM-107", p3),
        };
    }

    [Fact]
    public void Valid_selection_of_3_courses_with_distinct_professors_succeeds()
    {
        var courses = ThreeValid();
        var enrollment = Enrollment.Create(Guid.NewGuid(), "2026-1", courses);

        enrollment.CourseIds().Should().HaveCount(3);
        courses.Sum(c => c.Credits).Should().Be(9);
    }

    [Fact]
    public void More_than_3_courses_is_rejected()
    {
        var courses = ThreeValid();
        courses.Add(new Course("Extra", "EXT-999", Guid.NewGuid()));

        var act = () => Enrollment.Create(Guid.NewGuid(), "2026-1", courses);

        act.Should().Throw<InvalidEnrollmentException>()
            .Where(e => e.Code == ErrorCodes.EnrollmentCourseCountInvalid);
    }

    [Fact]
    public void Fewer_than_3_courses_is_rejected()
    {
        var courses = ThreeValid().Take(2).ToList();
        var act = () => Enrollment.Create(Guid.NewGuid(), "2026-1", courses);
        act.Should().Throw<InvalidEnrollmentException>()
            .Where(e => e.Code == ErrorCodes.EnrollmentCourseCountInvalid);
    }

    [Fact]
    public void Same_professor_twice_is_rejected()
    {
        var prof = Guid.NewGuid();
        var courses = new List<Course>
        {
            new("A", "A-01", prof),
            new("B", "B-02", prof),
            new("C", "C-03", Guid.NewGuid()),
        };
        var act = () => Enrollment.Create(Guid.NewGuid(), "2026-1", courses);
        act.Should().Throw<InvalidEnrollmentException>()
            .Where(e => e.Code == ErrorCodes.CourseProfessorConflict);
    }

    [Fact]
    public void Duplicated_course_is_rejected()
    {
        var courses = ThreeValid();
        var dup = new List<Course> { courses[0], courses[1], courses[0] };
        // Same instance twice -> same Id -> duplicated
        var act = () => EnrollmentRules.ValidateIds(dup.Select(c => c.Id).ToList());
        act.Should().Throw<InvalidEnrollmentException>()
            .Where(e => e.Code == ErrorCodes.CourseDuplicated);
    }

    [Fact]
    public void Replace_with_invalid_combination_is_rejected()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), "2026-1", ThreeValid());
        var prof = Guid.NewGuid();
        var invalid = new List<Course>
        {
            new("A", "A-01", prof),
            new("B", "B-02", prof),
            new("C", "C-03", Guid.NewGuid()),
        };
        var act = () => enrollment.ReplaceCourses(invalid);
        act.Should().Throw<InvalidEnrollmentException>()
            .Where(e => e.Code == ErrorCodes.CourseProfessorConflict);
        // Original selection preserved
        enrollment.CourseIds().Should().HaveCount(3);
    }

    [Fact]
    public void Student_name_must_be_valid()
    {
        var act = () => new Student("AB", "a@uni.edu", "123", Guid.NewGuid());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkDeleted_sets_flags_and_is_idempotent()
    {
        var student = new Student("Ana Gil", "ana@uni.edu", "1001", Guid.NewGuid());
        student.IsDeleted.Should().BeFalse();

        student.MarkDeleted();
        student.IsDeleted.Should().BeTrue();
        student.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        var deletedAt = student.DeletedAt;
        student.MarkDeleted();
        student.DeletedAt.Should().Be(deletedAt);

        var enrollment = Enrollment.Create(Guid.NewGuid(), "2026-1", ThreeValid());
        enrollment.MarkDeleted();
        enrollment.IsDeleted.Should().BeTrue();
        enrollment.DeletedAt.Should().NotBeNull();
    }
}
