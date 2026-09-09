using Domain.Entities;

namespace Application.Ports;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Student?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Student?> GetByDocumentAsync(string documentId, CancellationToken ct = default);
    Task<IReadOnlyList<Student>> ListAsync(CancellationToken ct = default);
    Task AddAsync(Student student, CancellationToken ct = default);
    void Remove(Student student);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}

public interface IProgramRepository
{
    Task<AcademicProgram?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AcademicProgram>> ListAsync(CancellationToken ct = default);
}

public interface IProfessorRepository
{
    Task<IReadOnlyList<Professor>> ListAsync(CancellationToken ct = default);
}

public interface ICourseRepository
{
    Task<IReadOnlyList<Course>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Course>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<Course?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByStudentAndPeriodAsync(Guid studentId, string period, CancellationToken ct = default);
    Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Enrollment>> ListByCourseAsync(Guid courseId, CancellationToken ct = default);
    Task AddAsync(Enrollment enrollment, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
