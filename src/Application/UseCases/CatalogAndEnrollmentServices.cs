using Application.Common;
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

    public async Task<PagedResult<ProgramDto>> ProgramsPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var req = PageRequest.Normalize(page, pageSize);
        var slice = await _programs.ListPagedAsync(req.Page, req.PageSize, ct);
        return new PagedResult<ProgramDto>(
            slice.Items.Select(p => new ProgramDto(p.Id, p.Name, p.Code)).ToList(), slice.Total);
    }

    public async Task<PagedResult<ProfessorDto>> ProfessorsPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var req = PageRequest.Normalize(page, pageSize);
        var slice = await _professors.ListPagedAsync(req.Page, req.PageSize, ct);
        return new PagedResult<ProfessorDto>(
            slice.Items.Select(p => new ProfessorDto(p.Id, p.FullName, p.Email)).ToList(), slice.Total);
    }

    public async Task<PagedResult<CourseDto>> CoursesPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var req = PageRequest.Normalize(page, pageSize);
        var slice = await _courses.ListPagedAsync(req.Page, req.PageSize, ct);
        var professors = await _professors.ListAsync(ct);
        var map = professors.ToDictionary(p => p.Id, p => p.FullName);
        return new PagedResult<CourseDto>(
            slice.Items.Select(c => new CourseDto(
                c.Id, c.Name, c.Code, c.Credits, c.ProfessorId,
                map.TryGetValue(c.ProfessorId, out var n) ? n : null)).ToList(), slice.Total);
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

        // Re-enrollment: a soft-deleted enrollment for the same period is restored
        // with the new selection (its unique index still occupies StudentId+Period).
        var deleted = await _enrollments.GetByStudentAndPeriodIncludingDeletedAsync(req.StudentId, req.Period.Trim(), ct);
        if (deleted is not null)
        {
            deleted.Restore();
            deleted.ReplaceCourses(courses); // domain revalidates all rules
            try
            {
                await _uow.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (IsConcurrency(ex))
            {
                throw new ConcurrencyException("The enrollment was modified by another request. Please retry.");
            }
            return await ToDtoAsync(deleted, student.FullName, ct);
        }

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
    /// Returns ONLY names of classmates sharing the course (privacy rule), paged.
    /// The requester must be enrolled in the course; otherwise 403.
    /// Students are loaded in a single batch query (no N+1).
    /// Empty page when nobody is enrolled.
    /// </summary>
    public async Task<PagedResult<ClassmateDto>> ClassmatesPagedAsync(
        Guid courseId, Guid requesterStudentId, int page, int pageSize, CancellationToken ct = default)
    {
        if (requesterStudentId == Guid.Empty)
            throw new DomainException(ErrorCodes.ValidationFailed, "Requester student is required.");
        if (!await _courses.ExistsAsync(courseId, ct))
            throw new EntityNotFoundException(ErrorCodes.CourseNotFound, "Course does not exist.");
        if (!await _enrollments.IsEnrolledAsync(requesterStudentId, courseId, ct))
            throw new ForbiddenException(ErrorCodes.ClassmatesForbidden,
                "Only students enrolled in this course can see their classmates.");

        var req = PageRequest.Normalize(page, pageSize);
        var slice = await _enrollments.ListByCoursePagedAsync(courseId, req.Page, req.PageSize, ct);
        if (slice.Total == 0) return new PagedResult<ClassmateDto>(new List<ClassmateDto>(), 0);

        var students = await _students.GetByIdsAsync(slice.Items.Select(e => e.StudentId).Distinct(), ct);
        var items = students
            .Select(s => new ClassmateDto(s.Id, s.FullName))
            .OrderBy(x => x.FullName)
            .ToList();
        return new PagedResult<ClassmateDto>(items, slice.Total);
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
