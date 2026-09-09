using Application.Common;
using Application.Ports;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class EfStudentRepository : IStudentRepository
{
    private readonly AppDbContext _db;
    public EfStudentRepository(AppDbContext db) => _db = db;

    public Task<Student?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Students.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Student?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) =>
        _db.Students.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Student?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Students.FirstOrDefaultAsync(s => s.Email == email.Trim().ToLower(), ct);

    public Task<Student?> GetByDocumentAsync(string documentId, CancellationToken ct = default) =>
        _db.Students.FirstOrDefaultAsync(s => s.DocumentId == documentId.Trim(), ct);

    public Task<Student?> GetByEmailIncludingDeletedAsync(string email, CancellationToken ct = default) =>
        _db.Students.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Email == email.Trim().ToLower(), ct);

    public Task<Student?> GetByDocumentIncludingDeletedAsync(string documentId, CancellationToken ct = default) =>
        _db.Students.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.DocumentId == documentId.Trim(), ct);

    public async Task<IReadOnlyList<Student>> ListAsync(CancellationToken ct = default) =>
        await _db.Students.OrderBy(s => s.FullName).ToListAsync(ct);

    public Task<PagedSlice<Student>> ListPagedAsync(int page, int pageSize, CancellationToken ct = default) =>
        EfPaging.ToSliceAsync(_db.Students.OrderBy(s => s.FullName), page, pageSize, ct);

    public async Task<IReadOnlyList<Student>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var list = ids.Distinct().ToList();
        return await _db.Students.Where(s => list.Contains(s.Id)).ToListAsync(ct);
    }

    public async Task AddAsync(Student student, CancellationToken ct = default) =>
        await _db.Students.AddAsync(student, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        _db.Students.AnyAsync(s => s.Id == id, ct);
}

public sealed class EfProgramRepository : IProgramRepository
{
    private readonly AppDbContext _db;
    public EfProgramRepository(AppDbContext db) => _db = db;

    public Task<AcademicProgram?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Programs.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<AcademicProgram>> ListAsync(CancellationToken ct = default) =>
        await _db.Programs.OrderBy(p => p.Name).ToListAsync(ct);

    public Task<PagedSlice<AcademicProgram>> ListPagedAsync(int page, int pageSize, CancellationToken ct = default) =>
        EfPaging.ToSliceAsync(_db.Programs.OrderBy(p => p.Name), page, pageSize, ct);
}

public sealed class EfProfessorRepository : IProfessorRepository
{
    private readonly AppDbContext _db;
    public EfProfessorRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Professor>> ListAsync(CancellationToken ct = default) =>
        await _db.Professors.OrderBy(p => p.FullName).ToListAsync(ct);

    public Task<PagedSlice<Professor>> ListPagedAsync(int page, int pageSize, CancellationToken ct = default) =>
        EfPaging.ToSliceAsync(_db.Professors.OrderBy(p => p.FullName), page, pageSize, ct);
}

public sealed class EfCourseRepository : ICourseRepository
{
    private readonly AppDbContext _db;
    public EfCourseRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Course>> ListAsync(CancellationToken ct = default) =>
        await _db.Courses.OrderBy(c => c.Code).ToListAsync(ct);

    public Task<PagedSlice<Course>> ListPagedAsync(int page, int pageSize, CancellationToken ct = default) =>
        EfPaging.ToSliceAsync(_db.Courses.OrderBy(c => c.Code), page, pageSize, ct);

    public async Task<IReadOnlyList<Course>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var list = ids.Distinct().ToList();
        return await _db.Courses.Where(c => list.Contains(c.Id)).ToListAsync(ct);
    }

    public Task<Course?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        _db.Courses.AnyAsync(c => c.Id == id, ct);
}

public sealed class EfEnrollmentRepository : IEnrollmentRepository
{
    private readonly AppDbContext _db;
    public EfEnrollmentRepository(AppDbContext db) => _db = db;

    public async Task<Enrollment?> GetByStudentAndPeriodAsync(Guid studentId, string period, CancellationToken ct = default)
    {
        var e = await _db.Enrollments.FirstOrDefaultAsync(x => x.StudentId == studentId && x.Period == period, ct);
        if (e is null) return null;
        await EnrollmentItemsLoader.LoadOneAsync(_db, e, ct);
        return e;
    }

    public async Task<Enrollment?> GetByStudentAndPeriodIncludingDeletedAsync(Guid studentId, string period, CancellationToken ct = default)
    {
        var e = await _db.Enrollments.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.StudentId == studentId && x.Period == period, ct);
        if (e is null) return null;
        await EnrollmentItemsLoader.LoadOneAsync(_db, e, ct);
        return e;
    }

    public async Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var e = await _db.Enrollments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return null;
        await EnrollmentItemsLoader.LoadOneAsync(_db, e, ct);
        return e;
    }

    public async Task<IReadOnlyList<Enrollment>> ListByCourseAsync(Guid courseId, CancellationToken ct = default)
    {
        var ids = await EnrollmentIdsByCourseAsync(courseId, ct);
        if (ids.Count == 0) return Array.Empty<Enrollment>();
        var enrollments = await _db.Enrollments.Where(e => ids.Contains(e.Id)).ToListAsync(ct);
        await EnrollmentItemsLoader.LoadManyAsync(_db, enrollments, ct);
        return enrollments;
    }

    public async Task<PagedSlice<Enrollment>> ListByCoursePagedAsync(Guid courseId, int page, int pageSize, CancellationToken ct = default)
    {
        var req = PageRequest.Normalize(page, pageSize);
        var ids = await EnrollmentIdsByCourseAsync(courseId, ct);
        // NOTE: count on the filtered Enrollments set (soft-deleted excluded by global filter),
        // not on raw link ids.
        var query = _db.Enrollments.Where(e => ids.Contains(e.Id)).OrderBy(e => e.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        await EnrollmentItemsLoader.LoadManyAsync(_db, items, ct);
        return new PagedSlice<Enrollment>(items, total);
    }

    public async Task<IReadOnlyList<Enrollment>> ListByStudentAsync(Guid studentId, CancellationToken ct = default)
    {
        var enrollments = await _db.Enrollments.Where(e => e.StudentId == studentId).ToListAsync(ct);
        await EnrollmentItemsLoader.LoadManyAsync(_db, enrollments, ct);
        return enrollments;
    }

    public async Task AddAsync(Enrollment enrollment, CancellationToken ct = default)
    {
        await _db.Enrollments.AddAsync(enrollment, ct);
        foreach (var item in enrollment.Items)
            await _db.EnrollmentCourses.AddAsync(item, ct);
    }

    private async Task<List<Guid>> EnrollmentIdsByCourseAsync(Guid courseId, CancellationToken ct) =>
        await _db.EnrollmentCourses
            .Where(x => x.CourseId == courseId)
            .Select(x => x.EnrollmentId)
            .Distinct()
            .ToListAsync(ct);
}

/// <summary>Single helper for SQL-level paging (COUNT + SKIP/TAKE).</summary>
internal static class EfPaging
{
    public static async Task<PagedSlice<T>> ToSliceAsync<T>(
        IOrderedQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        var req = PageRequest.Normalize(page, pageSize);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return new PagedSlice<T>(items, total);
    }
}

/// <summary>
/// Single place that hydrates the Enrollment.Items backing field.
/// Items are batch-loaded (one query for many enrollments) — no N+1.
/// </summary>
internal static class EnrollmentItemsLoader
{
    public static Task LoadOneAsync(AppDbContext db, Enrollment enrollment, CancellationToken ct) =>
        LoadManyAsync(db, new[] { enrollment }, ct);

    public static async Task LoadManyAsync(AppDbContext db, IReadOnlyList<Enrollment> enrollments, CancellationToken ct)
    {
        if (enrollments.Count == 0) return;
        var ids = enrollments.Select(e => e.Id).ToList();
        var allItems = await db.EnrollmentCourses
            .Where(x => ids.Contains(x.EnrollmentId))
            .ToListAsync(ct);
        foreach (var e in enrollments)
            SyncItems(e, allItems.Where(x => x.EnrollmentId == e.Id).ToList());
    }

    private static void SyncItems(Enrollment enrollment, List<EnrollmentCourse> items)
    {
        var field = typeof(Enrollment).GetField("_items",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field?.GetValue(enrollment) is List<EnrollmentCourse> list)
        {
            list.Clear();
            list.AddRange(items);
        }
    }
}

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public EfUnitOfWork(AppDbContext db) => _db = db;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Only sync items for enrollments that already exist in the database
        // (update path via ReplaceCourses). Newly added enrollments already
        // have their EnrollmentCourse items tracked by EfEnrollmentRepository.AddAsync.
        var enrollments = _db.ChangeTracker.Entries<Enrollment>()
            .Where(e => e.State is EntityState.Modified or EntityState.Unchanged)
            .Select(e => e.Entity)
            .ToList();

        foreach (var e in enrollments)
        {
            var trackedIds = e.CourseIds().ToHashSet();
            var dbItems = await _db.EnrollmentCourses
                .Where(x => x.EnrollmentId == e.Id)
                .ToListAsync(ct);

            foreach (var dbItem in dbItems.Where(x => !trackedIds.Contains(x.CourseId)))
                _db.EnrollmentCourses.Remove(dbItem);

            foreach (var courseId in trackedIds.Where(id => dbItems.All(x => x.CourseId != id)))
                await _db.EnrollmentCourses.AddAsync(new EnrollmentCourse(e.Id, courseId), ct);
        }

        return await _db.SaveChangesAsync(ct);
    }
}
