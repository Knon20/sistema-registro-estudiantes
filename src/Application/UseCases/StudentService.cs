using Application.DTOs;
using Application.Ports;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.UseCases;

public sealed class StudentService
{
    private readonly IStudentRepository _students;
    private readonly IProgramRepository _programs;
    private readonly IUnitOfWork _uow;

    public StudentService(IStudentRepository students, IProgramRepository programs, IUnitOfWork uow)
    {
        _students = students;
        _programs = programs;
        _uow = uow;
    }

    public async Task<StudentDto> CreateAsync(CreateStudentRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.FullName) || req.FullName.Trim().Length < 3)
            throw new DomainException(ErrorCodes.ValidationFailed, "FullName must have at least 3 characters.");
        var program = await _programs.GetByIdAsync(req.ProgramId, ct)
            ?? throw new EntityNotFoundException(ErrorCodes.ValidationFailed, "Program does not exist.");

        if (await _students.GetByEmailAsync(req.Email.Trim(), ct) is not null)
            throw new DuplicateEntityException(ErrorCodes.StudentDuplicated, "A student with the same email already exists.");
        if (await _students.GetByDocumentAsync(req.DocumentId.Trim(), ct) is not null)
            throw new DuplicateEntityException(ErrorCodes.StudentDuplicated, "A student with the same document already exists.");

        var student = new Student(req.FullName, req.Email, req.DocumentId, req.ProgramId);
        await _students.AddAsync(student, ct);
        await _uow.SaveChangesAsync(ct);
        return new StudentDto(student.Id, student.FullName, student.Email, student.DocumentId, student.ProgramId, program.Name, student.CreatedAt);
    }

    public async Task<StudentDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _students.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(ErrorCodes.StudentNotFound, "Student not found.");
        var program = await _programs.GetByIdAsync(s.ProgramId, ct);
        return new StudentDto(s.Id, s.FullName, s.Email, s.DocumentId, s.ProgramId, program?.Name, s.CreatedAt);
    }

    public async Task<IReadOnlyList<StudentDto>> ListAsync(CancellationToken ct = default)
    {
        var students = await _students.ListAsync(ct);
        var programs = await _programs.ListAsync(ct);
        var map = programs.ToDictionary(p => p.Id, p => p.Name);
        return students.Select(s => new StudentDto(
            s.Id, s.FullName, s.Email, s.DocumentId, s.ProgramId,
            map.TryGetValue(s.ProgramId, out var n) ? n : null, s.CreatedAt)).ToList();
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
        _students.Remove(s);
        await _uow.SaveChangesAsync(ct);
    }
}
