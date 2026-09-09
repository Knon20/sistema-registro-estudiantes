using Application.Common;
using Application.DTOs;
using Application.Ports;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.UseCases;

public sealed class StudentService
{
    private readonly IStudentRepository _students;
    private readonly IProgramRepository _programs;
    private readonly IEnrollmentRepository _enrollments;
    private readonly IUnitOfWork _uow;

    public StudentService(
        IStudentRepository students,
        IProgramRepository programs,
        IEnrollmentRepository enrollments,
        IUnitOfWork uow)
    {
        _students = students;
        _programs = programs;
        _enrollments = enrollments;
        _uow = uow;
    }

    public async Task<StudentDto> CreateAsync(CreateStudentRequest req, bool forceCreate = false, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.FullName) || req.FullName.Trim().Length < 3)
            throw new DomainException(ErrorCodes.ValidationFailed, "FullName must have at least 3 characters.");
        var program = await _programs.GetByIdAsync(req.ProgramId, ct)
            ?? throw new EntityNotFoundException(ErrorCodes.ValidationFailed, "Program does not exist.");

        if (await _students.GetByEmailAsync(req.Email.Trim(), ct) is not null)
            throw new DuplicateEntityException(ErrorCodes.StudentDuplicated, "A student with the same email already exists.");
        if (await _students.GetByDocumentAsync(req.DocumentId.Trim(), ct) is not null)
            throw new DuplicateEntityException(ErrorCodes.StudentDuplicated, "A student with the same document already exists.");

        if (!forceCreate)
        {
            // A soft-deleted record owns the email/document: don't fail silently,
            // let the client choose reactivate vs. create-new.
            // NOTE: sequential awaits — DbContext is not thread-safe (no Task.WhenAll).
            var byEmail = await _students.GetByEmailIncludingDeletedAsync(req.Email.Trim(), ct);
            var byDoc = await _students.GetByDocumentIncludingDeletedAsync(req.DocumentId.Trim(), ct);
            var deletedMatches = new[] { byEmail, byDoc }
                .Where(s => s is not null && s!.DeletedAt is not null)
                .GroupBy(s => s!.Id)
                .Select(g => g.First()!)
                .ToList();

            if (deletedMatches.Count > 1)
                throw new DuplicateEntityException(ErrorCodes.StudentDuplicated,
                    "Email and document belong to different deleted records. Contact support.");

            if (deletedMatches.Count == 1)
            {
                var match = deletedMatches[0];
                throw new DeletedStudentExistsException(new DeletedStudentInfo(
                    match.Id, match.FullName, match.Email, match.DeletedAt));
            }
        }

        var student = new Student(req.FullName, req.Email, req.DocumentId, req.ProgramId);
        await _students.AddAsync(student, ct);
        await _uow.SaveChangesAsync(ct);
        return new StudentDto(student.Id, student.FullName, student.Email, student.DocumentId, student.ProgramId, program.Name, student.CreatedAt);
    }

    /// <summary>Reactivates a soft-deleted student (its enrollments stay deleted).</summary>
    public async Task<StudentDto> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _students.GetByIdIncludingDeletedAsync(id, ct)
            ?? throw new EntityNotFoundException(ErrorCodes.StudentNotFound, "Student not found.");
        s.Restore();
        await _uow.SaveChangesAsync(ct);
        var program = await _programs.GetByIdAsync(s.ProgramId, ct);
        return new StudentDto(s.Id, s.FullName, s.Email, s.DocumentId, s.ProgramId, program?.Name, s.CreatedAt);
    }

    public async Task<StudentDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _students.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(ErrorCodes.StudentNotFound, "Student not found.");
        var program = await _programs.GetByIdAsync(s.ProgramId, ct);
        return new StudentDto(s.Id, s.FullName, s.Email, s.DocumentId, s.ProgramId, program?.Name, s.CreatedAt);
    }

    public async Task<PagedResult<StudentDto>> ListPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var req = PageRequest.Normalize(page, pageSize);
        var slice = await _students.ListPagedAsync(req.Page, req.PageSize, ct);
        var programs = await _programs.ListAsync(ct);
        var map = programs.ToDictionary(p => p.Id, p => p.Name);
        var items = slice.Items.Select(s => new StudentDto(
            s.Id, s.FullName, s.Email, s.DocumentId, s.ProgramId,
            map.TryGetValue(s.ProgramId, out var n) ? n : null, s.CreatedAt)).ToList();
        return new PagedResult<StudentDto>(items, slice.Total);
    }

    public async Task<StudentDto> UpdateAsync(Guid id, UpdateStudentRequest req, CancellationToken ct = default)
    {
        var s = await _students.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(ErrorCodes.StudentNotFound, "Student not found.");
        var program = await _programs.GetByIdAsync(req.ProgramId, ct)
            ?? throw new EntityNotFoundException(ErrorCodes.ValidationFailed, "Program does not exist.");

        var byEmail = await _students.GetByEmailAsync(req.Email.Trim(), ct);
        if (byEmail is not null && byEmail.Id != id)
            throw new DuplicateEntityException(ErrorCodes.StudentDuplicated, "Another student already uses this email.");

        s.Rename(req.FullName);
        s.SetEmail(req.Email);
        s.ChangeProgram(req.ProgramId);
        await _uow.SaveChangesAsync(ct);
        return new StudentDto(s.Id, s.FullName, s.Email, s.DocumentId, s.ProgramId, program.Name, s.CreatedAt);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _students.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(ErrorCodes.StudentNotFound, "Student not found.");
        // Soft delete with cascade in code: enrollments are marked deleted
        // in the same transaction (DB cascade only covers physical deletes).
        s.MarkDeleted();
        foreach (var e in await _enrollments.ListByStudentAsync(id, ct))
            e.MarkDeleted();
        await _uow.SaveChangesAsync(ct);
    }
}
