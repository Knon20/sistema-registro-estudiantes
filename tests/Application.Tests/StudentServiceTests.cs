using Application.DTOs;
using Application.Ports;
using Application.UseCases;
using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace Application.Tests;

public sealed class StudentServiceTests
{
    [Fact]
    public async Task Create_duplicate_email_fails()
    {
        var students = new Mock<IStudentRepository>();
        var programs = new Mock<IProgramRepository>();
        var enrollments = new Mock<IEnrollmentRepository>();
        var uow = new Mock<IUnitOfWork>();
        var programId = Guid.NewGuid();

        programs.Setup(p => p.GetByIdAsync(programId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicProgram("Ing. Sistemas", "IS-01"));
        students.Setup(s => s.GetByEmailAsync("a@uni.edu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student("Otro", "a@uni.edu", "999", programId));
        students.Setup(s => s.GetByDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        var svc = new StudentService(students.Object, programs.Object, enrollments.Object, uow.Object);
        var act = () => svc.CreateAsync(new CreateStudentRequest("Ana Gil", "a@uni.edu", "1001", programId));

        await act.Should().ThrowAsync<DuplicateEntityException>()
            .Where(e => e.Code == ErrorCodes.StudentDuplicated);
    }

    [Fact]
    public async Task Get_missing_student_throws_not_found()
    {
        var students = new Mock<IStudentRepository>();
        var programs = new Mock<IProgramRepository>();
        var enrollments = new Mock<IEnrollmentRepository>();
        var uow = new Mock<IUnitOfWork>();
        students.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        var svc = new StudentService(students.Object, programs.Object, enrollments.Object, uow.Object);
        var act = () => svc.GetAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .Where(e => e.Code == ErrorCodes.StudentNotFound);
    }

    [Fact]
    public async Task Delete_marks_student_and_enrollments_deleted_without_physical_remove()
    {
        var students = new Mock<IStudentRepository>();
        var programs = new Mock<IProgramRepository>();
        var enrollments = new Mock<IEnrollmentRepository>();
        var uow = new Mock<IUnitOfWork>();
        var programId = Guid.NewGuid();
        var student = new Student("Ana Gil", "ana@uni.edu", "1001", programId);
        var enrollment = Enrollment.Create(student.Id, "2026-1", TestData.ThreeValidCourses());

        students.Setup(s => s.GetByIdAsync(student.Id, It.IsAny<CancellationToken>())).ReturnsAsync(student);
        enrollments.Setup(e => e.ListByStudentAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { enrollment });

        var svc = new StudentService(students.Object, programs.Object, enrollments.Object, uow.Object);
        await svc.DeleteAsync(student.Id);

        student.DeletedAt.Should().NotBeNull();
        enrollment.DeletedAt.Should().NotBeNull();
        students.Verify(s => s.GetByIdAsync(student.Id, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_missing_student_throws_not_found()
    {
        var students = new Mock<IStudentRepository>();
        var programs = new Mock<IProgramRepository>();
        var enrollments = new Mock<IEnrollmentRepository>();
        var uow = new Mock<IUnitOfWork>();
        students.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        var svc = new StudentService(students.Object, programs.Object, enrollments.Object, uow.Object);
        var act = () => svc.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .Where(e => e.Code == ErrorCodes.StudentNotFound);
    }

    [Fact]
    public async Task Create_matching_deleted_record_offers_choice_instead_of_failing()
    {
        var students = new Mock<IStudentRepository>();
        var programs = new Mock<IProgramRepository>();
        var enrollments = new Mock<IEnrollmentRepository>();
        var uow = new Mock<IUnitOfWork>();
        var programId = Guid.NewGuid();
        var deleted = new Student("Juan Perez", "juan.perez@gmail.com", "123456", programId);
        deleted.MarkDeleted();

        programs.Setup(p => p.GetByIdAsync(programId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicProgram("Admin", "AD-02"));
        students.Setup(s => s.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);
        students.Setup(s => s.GetByDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);
        students.Setup(s => s.GetByEmailIncludingDeletedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleted);
        students.Setup(s => s.GetByDocumentIncludingDeletedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleted);

        var svc = new StudentService(students.Object, programs.Object, enrollments.Object, uow.Object);
        var act = () => svc.CreateAsync(new CreateStudentRequest("Juan Perez", "juan.perez@gmail.com", "123456", programId));

        var ex = await act.Should().ThrowAsync<DeletedStudentExistsException>();
        ex.Which.Candidate.Id.Should().Be(deleted.Id);
        ex.Which.Code.Should().Be(ErrorCodes.StudentDeletedExists);
        students.Verify(s => s.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_with_force_creates_new_id_despite_deleted_match()
    {
        var students = new Mock<IStudentRepository>();
        var programs = new Mock<IProgramRepository>();
        var enrollments = new Mock<IEnrollmentRepository>();
        var uow = new Mock<IUnitOfWork>();
        var programId = Guid.NewGuid();
        var deleted = new Student("Juan Perez", "juan.perez@gmail.com", "123456", programId);
        deleted.MarkDeleted();

        programs.Setup(p => p.GetByIdAsync(programId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicProgram("Admin", "AD-02"));
        students.Setup(s => s.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);
        students.Setup(s => s.GetByDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);
        students.Setup(s => s.GetByEmailIncludingDeletedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleted);
        students.Setup(s => s.GetByDocumentIncludingDeletedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleted);

        var svc = new StudentService(students.Object, programs.Object, enrollments.Object, uow.Object);
        var result = await svc.CreateAsync(
            new CreateStudentRequest("Juan Perez", "juan.perez@gmail.com", "123456", programId), forceCreate: true);

        result.Id.Should().NotBe(deleted.Id);
        students.Verify(s => s.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_with_email_and_document_from_different_deleted_records_fails()
    {
        var students = new Mock<IStudentRepository>();
        var programs = new Mock<IProgramRepository>();
        var enrollments = new Mock<IEnrollmentRepository>();
        var uow = new Mock<IUnitOfWork>();
        var programId = Guid.NewGuid();
        var deletedA = new Student("Ana Ruiz", "a@uni.edu", "111", programId); deletedA.MarkDeleted();
        var deletedB = new Student("Beto Gil", "b@uni.edu", "222", programId); deletedB.MarkDeleted();

        programs.Setup(p => p.GetByIdAsync(programId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicProgram("Admin", "AD-02"));
        students.Setup(s => s.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);
        students.Setup(s => s.GetByDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);
        students.Setup(s => s.GetByEmailIncludingDeletedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedA);
        students.Setup(s => s.GetByDocumentIncludingDeletedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedB);

        var svc = new StudentService(students.Object, programs.Object, enrollments.Object, uow.Object);
        var act = () => svc.CreateAsync(new CreateStudentRequest("Camilo Paz", "a@uni.edu", "222", programId));

        await act.Should().ThrowAsync<DuplicateEntityException>()
            .Where(e => e.Code == ErrorCodes.StudentDuplicated);
    }

    [Fact]
    public async Task Restore_reactivates_deleted_student()
    {
        var students = new Mock<IStudentRepository>();
        var programs = new Mock<IProgramRepository>();
        var enrollments = new Mock<IEnrollmentRepository>();
        var uow = new Mock<IUnitOfWork>();
        var programId = Guid.NewGuid();
        var deleted = new Student("Juan Perez", "juan.perez@gmail.com", "123456", programId);
        deleted.MarkDeleted();

        students.Setup(s => s.GetByIdIncludingDeletedAsync(deleted.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleted);
        programs.Setup(p => p.GetByIdAsync(programId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicProgram("Admin", "AD-02"));

        var svc = new StudentService(students.Object, programs.Object, enrollments.Object, uow.Object);
        var result = await svc.RestoreAsync(deleted.Id);

        result.Id.Should().Be(deleted.Id);
        deleted.DeletedAt.Should().BeNull();
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Restore_missing_student_throws_not_found()
    {
        var students = new Mock<IStudentRepository>();
        var programs = new Mock<IProgramRepository>();
        var enrollments = new Mock<IEnrollmentRepository>();
        var uow = new Mock<IUnitOfWork>();
        students.Setup(s => s.GetByIdIncludingDeletedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        var svc = new StudentService(students.Object, programs.Object, enrollments.Object, uow.Object);
        var act = () => svc.RestoreAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .Where(e => e.Code == ErrorCodes.StudentNotFound);
    }
}
