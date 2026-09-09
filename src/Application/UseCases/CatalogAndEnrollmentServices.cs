using Application.DTOs;
using Application.Ports;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.UseCases;

public sealed class CatalogService
{
    private readonly IProgramRepository _programs;
    private readonly IProfessorRepository _professors;
    private readonly ICourseRepository _courses;

    public CatalogService(IProgramRepository programs, IProfessorRepository professors, ICourseRepository courses)
    {
        _programs = programs;
        _professors = professors;
        _courses = courses;
    }

    public async Task<IReadOnlyList<ProgramDto>> ProgramsAsync(CancellationToken ct = default) =>
        (await _programs.ListAsync(ct)).Select(p => new ProgramDto(p.Id, p.Name, p.Code)).ToList();

    public async Task<IReadOnlyList<ProfessorDto>> ProfessorsAsync(CancellationToken ct = default) =>
        (await _professors.ListAsync(ct)).Select(p => new ProfessorDto(p.Id, p.FullName, p.Email)).ToList();

    public async Task<IReadOnlyList<CourseDto>> CoursesAsync(CancellationToken ct = default)
    {
        var courses = await _courses.ListAsync(ct);
        var professors = await _professors.ListAsync(ct);
        var map = professors.ToDictionary(p => p.Id, p => p.FullName);
        return courses.Select(c => new CourseDto(
            c.Id, c.Name, c.Code, c.Credits, c.ProfessorId,
            map.TryGetValue(c.ProfessorId, out var n) ? n : null)).ToList();
    }
}

public sealed class EnrollmentService
{
    private readonly IStudentRepository _students;
    private readonly ICourseRepository _courses;
    private readonly IProfessorRepository _professors;
    private readonly IEnrollmentRepository _enrollments;
    private readonly IUnitOfWork _uow;

    public EnrollmentService(
        IStudentRepository students,
        ICourseRepository courses,
        IProfessorRepository professors,
        IEnrollmentRepository enrollments,
        IUnitOfWork uow)
    {
        _students = students;
        _courses = courses;
        _professors = professors;
        _enrollments = enrollments;
        _uow = uow;
    }

    public async Task<EnrollmentDto> CreateAsync(CreateEnrollmentRequest req, CancellationToken ct = default)
    {
        var student = await _students.GetByIdAsync(req.StudentId, ct)
            ?? throw new EntityNotFoundException(ErrorCodes.StudentNotFound, "Student not found.");

        EnrollmentRules.ValidateIds(req.CourseIds);

        var existing = await _enrollments.GetByStudentAndPeriodAsync(req.StudentId, req.Period.Trim(), ct);
        if (existing is not null)
            throw new DuplicateEntityException(ErrorCodes.EnrollmentDuplicated, "The student already has an active enrollment for this period.");

        var courses = (await _courses.GetByIdsAsync(req.CourseIds, ct)).ToList();
        if (courses.Count != req.CourseIds.Count)
            throw new EntityNotFoundException(ErrorCodes.CourseNotFound, "One or more courses do not exist.");

        // Aggregate protects invariants (count, duplicates, professor conflict, credits).
        var enrollment = Enrollment.Create(req.StudentId, req.Period, courses);

        await _enrollments.AddAsync(enrollment, ct);
        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (ConcurrencyException) { throw; }
        catch (Exception ex) when (IsConcurrency(ex))
        {
            throw new ConcurrencyException("The enrollment was modified by another request. Please retry.");
        }

        return await ToDtoAsync(enrollment, student.FullName, ct);
    }

    public async Task<EnrollmentDto> UpdateAsync(Guid studentId, string period, UpdateEnrollmentRequest req, CancellationToken ct = default)
    {
        var student = await _students.GetByIdAsync(studentId, ct)
            ?? throw new EntityNotFoundException(ErrorCodes.StudentNotFound, "Student not found.");

        EnrollmentRules.ValidateIds(req.CourseIds);

        var enrollment = await _enrollments.GetByStudentAndPeriodAsync(studentId, period.Trim(), ct)
            ?? throw new EntityNotFoundException(ErrorCodes.EnrollmentNotFound, "Enrollment not found.");

        var courses = (await _courses.GetByIdsAsync(req.CourseIds, ct)).ToList();
        if (courses.Count != req.CourseIds.Count)
            throw new EntityNotFoundException(ErrorCodes.CourseNotFound, "One or more courses do not exist.");

        enrollment.ReplaceCourses(courses);

        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (IsConcurrency(ex))
        {
            throw new ConcurrencyException("The enrollment was modified by another request. Please retry.");
        }

        return await ToDtoAsync(enrollment, student.FullName, ct);
    }

    public async Task<EnrollmentDto?> GetAsync(Guid studentId, string period, CancellationToken ct = default)
    {
        var student = await _students.GetByIdAsync(studentId, ct)
            ?? throw new EntityNotFoundException(ErrorCodes.StudentNotFound, "Student not found.");
        var enrollment = await _enrollments.GetByStudentAndPeriodAsync(studentId, period.Trim(), ct);
        if (enrollment is null) return null;
        return await ToDtoAsync(enrollment, student.FullName, ct);
    }

    /// <summary>
    /// Returns ONLY names of classmates sharing the course (privacy rule).
    /// Empty list when nobody is enrolled.
    /// </summary>
    public async Task<IReadOnlyList<ClassmateDto>> ClassmatesAsync(Guid courseId, CancellationToken ct = default)
    {
        if (!await _courses.ExistsAsync(courseId, ct))
            throw new EntityNotFoundException(ErrorCodes.CourseNotFound, "Course does not exist.");

        var enrollments = await _enrollments.ListByCourseAsync(courseId, ct);
        if (enrollments.Count == 0) return Array.Empty<ClassmateDto>();

        var result = new List<ClassmateDto>();
        foreach (var e in enrollments)
        {
            var s = await _students.GetByIdAsync(e.StudentId, ct);
            if (s is not null) result.Add(new ClassmateDto(s.Id, s.FullName));
        }
        return result.OrderBy(x => x.FullName).ToList();
    }

    private async Task<EnrollmentDto> ToDtoAsync(Enrollment enrollment, string studentName, CancellationToken ct)
    {
        var allCourses = await _courses.GetByIdsAsync(enrollment.CourseIds(), ct);
        var professors = await _professors.ListAsync(ct);
        var pmap = professors.ToDictionary(p => p.Id, p => p.FullName);
        var items = allCourses.Select(c => new CourseDto(
            c.Id, c.Name, c.Code, c.Credits, c.ProfessorId,
            pmap.TryGetValue(c.ProfessorId, out var n) ? n : null)).ToList();
        return new EnrollmentDto(enrollment.Id, enrollment.StudentId, studentName, enrollment.Period,
            items.Sum(i => i.Credits), items, enrollment.CreatedAt, enrollment.UpdatedAt);
    }

    private static bool IsConcurrency(Exception ex) =>
        ex.GetType().Name.Contains("DbUpdateConcurrency", StringComparison.OrdinalIgnoreCase);
}
