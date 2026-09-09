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

        student.IsDeleted.Should().BeTrue();
        student.DeletedAt.Should().NotBeNull();
        enrollment.IsDeleted.Should().BeTrue();
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
}
