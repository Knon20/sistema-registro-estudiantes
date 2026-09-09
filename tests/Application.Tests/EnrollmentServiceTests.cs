using Application.Common;
using Application.DTOs;
using Application.Ports;
using Application.UseCases;
using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace Application.Tests;

public sealed class EnrollmentServiceTests
{
    private static (Mock<IStudentRepository>, Mock<ICourseRepository>, Mock<IProfessorRepository>, Mock<IEnrollmentRepository>, Mock<IUnitOfWork>) Mocks()
        => (new(), new(), new(), new(), new());

    private static List<Course> Courses(string suffix = "")
    {
        var p1 = Guid.NewGuid(); var p2 = Guid.NewGuid(); var p3 = Guid.NewGuid();
        return new List<Course>
        {
            new($"C1{suffix}", $"C1{suffix}-01", p1),
            new($"C2{suffix}", $"C2{suffix}-02", p2),
            new($"C3{suffix}", $"C3{suffix}-03", p3),
        };
    }

    [Fact]
    public async Task Create_enrollment_success()
    {
        var (students, courses, professors, enrollments, uow) = Mocks();
        var studentId = Guid.NewGuid();
        var student = new Student("Juan Pérez", "juan@uni.edu", "1001", Guid.NewGuid());
        var list = Courses();

        students.Setup(s => s.GetByIdAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync(student);
        enrollments.Setup(e => e.GetByStudentAndPeriodAsync(studentId, "2026-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enrollment?)null);
        courses.Setup(c => c.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        professors.Setup(p => p.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Professor>());

        var svc = new EnrollmentService(students.Object, courses.Object, professors.Object, enrollments.Object, uow.Object);
        var dto = await svc.CreateAsync(new CreateEnrollmentRequest(studentId, "2026-1", list.Select(c => c.Id).ToList()));

        dto.TotalCredits.Should().Be(9);
        dto.Courses.Should().HaveCount(3);
        enrollments.Verify(e => e.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_enrollment_with_professor_conflict_fails()
    {
        var (students, courses, professors, enrollments, uow) = Mocks();
        var studentId = Guid.NewGuid();
        var student = new Student("Juan Pérez", "juan@uni.edu", "1001", Guid.NewGuid());
        var prof = Guid.NewGuid();
        var list = new List<Course>
        {
            new("A", "A-01", prof), new("B", "B-02", prof), new("C", "C-03", Guid.NewGuid())
        };

        students.Setup(s => s.GetByIdAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync(student);
        enrollments.Setup(e => e.GetByStudentAndPeriodAsync(studentId, "2026-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enrollment?)null);
        courses.Setup(c => c.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        professors.Setup(p => p.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Professor>());

        var svc = new EnrollmentService(students.Object, courses.Object, professors.Object, enrollments.Object, uow.Object);
        var act = () => svc.CreateAsync(new CreateEnrollmentRequest(studentId, "2026-1", list.Select(c => c.Id).ToList()));

        await act.Should().ThrowAsync<InvalidEnrollmentException>()
            .Where(e => e.Code == ErrorCodes.CourseProfessorConflict);
    }

    [Fact]
    public async Task Create_enrollment_with_missing_course_fails()
    {
        var (students, courses, professors, enrollments, uow) = Mocks();
        var studentId = Guid.NewGuid();
        var student = new Student("Ana Gil", "ana@uni.edu", "1002", Guid.NewGuid());

        students.Setup(s => s.GetByIdAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync(student);
        enrollments.Setup(e => e.GetByStudentAndPeriodAsync(studentId, "2026-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enrollment?)null);
        courses.Setup(c => c.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Course> { Courses()[0] }); // only 1 found of 3 requested

        var svc = new EnrollmentService(students.Object, courses.Object, professors.Object, enrollments.Object, uow.Object);
        var act = () => svc.CreateAsync(new CreateEnrollmentRequest(studentId, "2026-1",
            new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }));

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .Where(e => e.Code == ErrorCodes.CourseNotFound);
    }

    [Fact]
    public async Task Duplicate_enrollment_for_same_period_fails()
    {
        var (students, courses, professors, enrollments, uow) = Mocks();
        var studentId = Guid.NewGuid();
        var student = new Student("Ana Gil", "ana@uni.edu", "1002", Guid.NewGuid());
        var list = Courses();

        students.Setup(s => s.GetByIdAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync(student);
        enrollments.Setup(e => e.GetByStudentAndPeriodAsync(studentId, "2026-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enrollment.Create(studentId, "2026-1", list));

        var svc = new EnrollmentService(students.Object, courses.Object, professors.Object, enrollments.Object, uow.Object);
        var act = () => svc.CreateAsync(new CreateEnrollmentRequest(studentId, "2026-1", list.Select(c => c.Id).ToList()));

        await act.Should().ThrowAsync<DuplicateEntityException>()
            .Where(e => e.Code == ErrorCodes.EnrollmentDuplicated);
    }

    [Fact]
    public async Task Classmates_returns_only_names_and_empty_when_no_enrollments()
    {
        var (students, courses, professors, enrollments, uow) = Mocks();
        var courseId = Guid.NewGuid();
        courses.Setup(c => c.ExistsAsync(courseId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        enrollments.Setup(e => e.ListByCoursePagedAsync(courseId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedSlice<Enrollment>(new List<Enrollment>(), 0));

        var svc = new EnrollmentService(students.Object, courses.Object, professors.Object, enrollments.Object, uow.Object);
        var result = await svc.ClassmatesPagedAsync(courseId, 1, 20);
        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        students.Verify(s => s.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Classmates_loads_students_in_single_batch_query()
    {
        var (students, courses, professors, enrollments, uow) = Mocks();
        var courseId = Guid.NewGuid();
        var list = Courses();
        var e1 = Enrollment.Create(Guid.NewGuid(), "2026-1", list);
        var e2 = Enrollment.Create(Guid.NewGuid(), "2026-1", list);
        var programId = Guid.NewGuid();
        var s1 = new Student("Ana Gil", "ana@uni.edu", "1", programId);
        var s2 = new Student("Luis Paz", "luis@uni.edu", "2", programId);

        courses.Setup(c => c.ExistsAsync(courseId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        enrollments.Setup(e => e.ListByCoursePagedAsync(courseId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedSlice<Enrollment>(new List<Enrollment> { e1, e2 }, 2));
        // Map enrollments to our students regardless of generated ids
        students.Setup(s => s.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Student> { s1, s2 });

        var svc = new EnrollmentService(students.Object, courses.Object, professors.Object, enrollments.Object, uow.Object);
        var result = await svc.ClassmatesPagedAsync(courseId, 1, 20);

        result.Total.Should().Be(2);
        result.Items.Select(i => i.FullName).Should().BeEquivalentTo("Ana Gil", "Luis Paz");
        // One batch call for all students — no N+1
        students.Verify(s => s.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Classmates_unknown_course_throws_not_found()
    {
        var (students, courses, professors, enrollments, uow) = Mocks();
        var courseId = Guid.NewGuid();
        courses.Setup(c => c.ExistsAsync(courseId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var svc = new EnrollmentService(students.Object, courses.Object, professors.Object, enrollments.Object, uow.Object);
        var act = () => svc.ClassmatesPagedAsync(courseId, 1, 20);

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .Where(e => e.Code == ErrorCodes.CourseNotFound);
    }
}
