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
        var uow = new Mock<IUnitOfWork>();
        var programId = Guid.NewGuid();

        programs.Setup(p => p.GetByIdAsync(programId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicProgram("Ing. Sistemas", "IS-01"));
        students.Setup(s => s.GetByEmailAsync("a@uni.edu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student("Otro", "a@uni.edu", "999", programId));
        students.Setup(s => s.GetByDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        var svc = new StudentService(students.Object, programs.Object, uow.Object);
        var act = () => svc.CreateAsync(new CreateStudentRequest("Ana Gil", "a@uni.edu", "1001", programId));

        await act.Should().ThrowAsync<DuplicateEntityException>()
            .Where(e => e.Code == ErrorCodes.StudentDuplicated);
    }

    [Fact]
    public async Task Get_missing_student_throws_not_found()
    {
        var students = new Mock<IStudentRepository>();
        var programs = new Mock<IProgramRepository>();
        var uow = new Mock<IUnitOfWork>();
        students.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        var svc = new StudentService(students.Object, programs.Object, uow.Object);
        var act = () => svc.GetAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .Where(e => e.Code == ErrorCodes.StudentNotFound);
    }
}
