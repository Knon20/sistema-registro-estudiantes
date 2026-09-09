using Domain.Exceptions;

namespace Domain.Exceptions;

public static class ErrorCodes
{
    public const string EnrollmentCourseCountInvalid = "ENROLLMENT_COURSE_COUNT_INVALID";
    public const string CourseDuplicated = "COURSE_DUPLICATED";
    public const string CourseProfessorConflict = "COURSE_PROFESSOR_CONFLICT";
    public const string CreditsInvalid = "CREDITS_INVALID";
    public const string CourseNotFound = "COURSE_NOT_FOUND";
    public const string StudentNotFound = "STUDENT_NOT_FOUND";
    public const string StudentDuplicated = "STUDENT_DUPLICATED";
    public const string StudentDeletedExists = "STUDENT_DELETED_EXISTS";
    public const string EnrollmentDuplicated = "ENROLLMENT_DUPLICATED";
    public const string EnrollmentNotFound = "ENROLLMENT_NOT_FOUND";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string ValidationFailed = "VALIDATION_FAILED";
}

public sealed class InvalidEnrollmentException : DomainException
{
    public InvalidEnrollmentException(string code, string message) : base(code, message) { }
}

public sealed class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string code, string message) : base(code, message) { }
}

public sealed class DuplicateEntityException : DomainException
{
    public DuplicateEntityException(string code, string message) : base(code, message) { }
}

/// <summary>
/// A soft-deleted record owns the email/document. Carries the candidate
/// so the client can offer reactivate-vs-create-new.
/// </summary>
public sealed class DeletedStudentExistsException : DomainException
{
    public DeletedStudentInfo Candidate { get; }

    public DeletedStudentExistsException(DeletedStudentInfo candidate)
        : base(ErrorCodes.StudentDeletedExists,
              $"A deleted record for '{candidate.FullName}' ({candidate.Email}) already exists. You can reactivate it or create a new one.")
    {
        Candidate = candidate;
    }
}

public sealed record DeletedStudentInfo(Guid Id, string FullName, string Email, DateTime? DeletedAt);

public sealed class ConcurrencyException : DomainException
{
    public ConcurrencyException(string message) : base(ErrorCodes.ConcurrencyConflict, message) { }
}
