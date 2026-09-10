using Application.Common;
using Application.Ports;
using Application.UseCases;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.Tests;

public sealed class PagingServiceTests
{
    [Fact]
    public async Task Students_list_paged_maps_items_and_total()
    {
        var students = new Mock<IStudentRepository>();
        var programs = new Mock<IProgramRepository>();
        var enrollments = new Mock<IEnrollmentRepository>();
        var uow = new Mock<IUnitOfWork>();
        var programId = Guid.NewGuid();
        var items = new List<Student>
        {
            new("Ana Gil", "ana@uni.edu", "1", programId),
            new("Luis Paz", "luis@uni.edu", "2", programId),
        };

        students.Setup(s => s.ListPagedAsync(2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedSlice<Student>(items, 25));
        programs.Setup(p => p.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AcademicProgram> { new("Ing. Sistemas", "IS-01") });

        var svc = new StudentService(students.Object, programs.Object, enrollments.Object, uow.Object);
        var result = await svc.ListPagedAsync(2, 10);

        result.Total.Should().Be(25);
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.FullName).Should().BeEquivalentTo("Ana Gil", "Luis Paz");
    }

    [Fact]
    public async Task Students_list_paged_normalizes_out_of_range_input()
    {
        var students = new Mock<IStudentRepository>();
        var programs = new Mock<IProgramRepository>();
        var enrollments = new Mock<IEnrollmentRepository>();
        var uow = new Mock<IUnitOfWork>();

        students.Setup(s => s.ListPagedAsync(1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedSlice<Student>(new List<Student>(), 0));
        programs.Setup(p => p.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AcademicProgram>());

        var svc = new StudentService(students.Object, programs.Object, enrollments.Object, uow.Object);
        await svc.ListPagedAsync(0, 500);

        students.Verify(s => s.ListPagedAsync(1, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Courses_paged_returns_slice_with_professor_names()
    {
        var programs = new Mock<IProgramRepository>();
        var professors = new Mock<IProfessorRepository>();
        var courses = new Mock<ICourseRepository>();
        var profId = Guid.NewGuid();
        var courseItems = new List<Course> { new("Cálculo I", "MAT-101", profId) };

        courses.Setup(c => c.ListPagedAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedSlice<Course>(courseItems, 10));
        professors.Setup(p => p.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Professor> { new("Ana Torres", "ana@uni.edu") });

        var svc = new CatalogService(programs.Object, professors.Object, courses.Object);
        var result = await svc.CoursesPagedAsync(1, 20);

        result.Total.Should().Be(10);
        result.Items.Should().HaveCount(1);
        result.Items[0].Code.Should().Be("MAT-101");
    }

        [Fact]
    public async Task Programs_and_professors_paged_forward_total()
    {        var programs = new Mock<IProgramRepository>();
        var professors = new Mock<IProfessorRepository>();
        var courses = new Mock<ICourseRepository>();

        programs.Setup(p => p.ListPagedAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedSlice<AcademicProgram>(new List<AcademicProgram> { new("IS", "IS-01") }, 2));
        professors.Setup(p => p.ListPagedAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedSlice<Professor>(
                Enumerable.Range(1, 5).Select(i => new Professor($"P{i}", $"p{i}@uni.edu")).ToList(), 5));

        var svc = new CatalogService(programs.Object, professors.Object, courses.Object);
        (await svc.ProgramsPagedAsync(1, 20)).Total.Should().Be(2);
        (await svc.ProfessorsPagedAsync(1, 20)).Total.Should().Be(5);
    }
}
