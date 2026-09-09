using Domain.Entities;
using Domain.Exceptions;

namespace Domain.Entities;

/// <summary>
/// Pure domain rules. No EF, no ASP.NET, no I/O. Fully unit-testable.
/// </summary>
public static class EnrollmentRules
{
    public static void ValidateSelection(IReadOnlyList<Course>? courses)
    {
        if (courses is null || courses.Count != Enrollment.RequiredCourses)
            throw new InvalidEnrollmentException(
                ErrorCodes.EnrollmentCourseCountInvalid,
                $"An enrollment must contain exactly {Enrollment.RequiredCourses} courses.");

        var ids = courses.Select(c => c.Id).ToList();
        if (ids.Distinct().Count() != ids.Count)
            throw new InvalidEnrollmentException(
                ErrorCodes.CourseDuplicated,
                "The same course cannot be selected twice.");

        var professorIds = courses.Select(c => c.ProfessorId).ToList();
        if (professorIds.Distinct().Count() != professorIds.Count)
            throw new InvalidEnrollmentException(
                ErrorCodes.CourseProfessorConflict,
                "The selected courses cannot be taken because two of them are taught by the same professor.");

        var total = courses.Sum(c => c.Credits);
        if (total != Enrollment.RequiredCredits)
            throw new InvalidEnrollmentException(
                ErrorCodes.CreditsInvalid,
                $"An enrollment must total {Enrollment.RequiredCredits} credits, but got {total}.");
    }

    public static void ValidateIds(IReadOnlyList<Guid>? courseIds)
    {
        if (courseIds is null || courseIds.Count != Enrollment.RequiredCourses)
            throw new InvalidEnrollmentException(
                ErrorCodes.EnrollmentCourseCountInvalid,
                $"An enrollment must contain exactly {Enrollment.RequiredCourses} courses.");
        if (courseIds.Distinct().Count() != courseIds.Count)
            throw new InvalidEnrollmentException(
                ErrorCodes.CourseDuplicated,
                "The same course cannot be selected twice.");
    }
}
