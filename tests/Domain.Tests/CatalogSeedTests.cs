using Domain.ReferenceData;
using FluentAssertions;

namespace Domain.Tests;

public sealed class CatalogSeedTests
{
    [Fact]
    public void Exactly_5_professors_with_unique_valid_emails()
    {
        CatalogSeed.Professors.Should().HaveCount(5);
        CatalogSeed.Professors.Select(p => p.Email).Distinct().Should().HaveCount(5);
        CatalogSeed.Professors.Should().OnlyContain(p =>
            !string.IsNullOrWhiteSpace(p.FullName) && p.Email.Contains('@'));
    }

    [Fact]
    public void Exactly_10_courses_with_unique_codes()
    {
        CatalogSeed.Courses.Should().HaveCount(10);
        CatalogSeed.Courses.Select(c => c.Code).Distinct().Should().HaveCount(10);
    }

    [Fact]
    public void Each_professor_teaches_exactly_2_courses()
    {
        var groups = CatalogSeed.Courses.GroupBy(c => c.ProfessorEmail).ToList();
        groups.Should().HaveCount(5);
        groups.Should().OnlyContain(g => g.Count() == 2);
        groups.Select(g => g.Key).Should().BeEquivalentTo(CatalogSeed.Professors.Select(p => p.Email));
    }

    [Fact]
    public void Programs_have_unique_codes()
    {
        CatalogSeed.Programs.Should().HaveCountGreaterThanOrEqualTo(1);
        CatalogSeed.Programs.Select(p => p.Code).Distinct().Should().HaveCount(CatalogSeed.Programs.Count);
    }
}
