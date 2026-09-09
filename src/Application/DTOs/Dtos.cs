namespace Application.DTOs;

public sealed record CreateStudentRequest(string FullName, string Email, string DocumentId, Guid ProgramId);
public sealed record UpdateStudentRequest(string FullName, string Email, Guid ProgramId);
public sealed record StudentDto(Guid Id, string FullName, string Email, string DocumentId, Guid ProgramId, string? ProgramName, DateTime CreatedAt);
public sealed record ProgramDto(Guid Id, string Name, string Code);
public sealed record ProfessorDto(Guid Id, string FullName, string Email);
public sealed record CourseDto(Guid Id, string Name, string Code, int Credits, Guid ProfessorId, string? ProfessorName);
public sealed record CreateEnrollmentRequest(Guid StudentId, string Period, List<Guid> CourseIds);
public sealed record UpdateEnrollmentRequest(List<Guid> CourseIds);
public sealed record EnrollmentDto(Guid Id, Guid StudentId, string StudentName, string Period, int TotalCredits, List<CourseDto> Courses, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record ClassmateDto(Guid StudentId, string FullName);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);
