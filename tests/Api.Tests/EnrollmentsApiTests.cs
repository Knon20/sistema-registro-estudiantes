using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Api.Tests;

[Collection("api")]
public sealed class EnrollmentsApiTests
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public EnrollmentsApiTests(ApiFixture fixture) => _client = fixture.Client;

    private async Task<(Guid StudentId, List<CourseDto> Courses)> SetupAsync(string tag)
    {
        var programs = await _client.GetFromJsonAsync<Paged<ProgramDto>>("/api/programs?page=1&pageSize=10", Json);
        var studentRes = await _client.PostAsJsonAsync("/api/students", new
        {
            fullName = $"Enroll {tag}",
            email = $"enroll.{tag}.{Guid.NewGuid():N}@uni.edu",
            documentId = $"E-{tag}-{Guid.NewGuid():N}"[..20],
            programId = programs!.Items[0].Id
        });
        var student = await studentRes.Content.ReadFromJsonAsync<StudentDto>(Json);
        var courses = await _client.GetFromJsonAsync<Paged<CourseDto>>("/api/courses?page=1&pageSize=100", Json);
        return (student!.Id, courses!.Items);
    }

    private sealed record ProgramDto(Guid Id, string Name, string Code);

    private static List<Guid> ValidPick(List<CourseDto> courses) => new()
    {
        courses.Single(c => c.Code == "MAT-101").Id,
        courses.Single(c => c.Code == "SIS-103").Id,
        courses.Single(c => c.Code == "HUM-107").Id,
    };

    [Fact]
    public async Task Enrollment_Valid_Returns201_With9Credits()
    {
        var (studentId, courses) = await SetupAsync("valid");
        var res = await _client.PostAsJsonAsync("/api/enrollments",
            new { studentId, period = "2026-1", courseIds = ValidPick(courses) });

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await res.Content.ReadFromJsonAsync<EnrollmentDto>(Json);
        dto!.TotalCredits.Should().Be(9);
        dto.Courses.Should().HaveCount(3);
    }

    [Fact]
    public async Task Enrollment_SameProfessor_Returns422()
    {
        var (studentId, courses) = await SetupAsync("conflict");
        var res = await _client.PostAsJsonAsync("/api/enrollments", new
        {
            studentId,
            period = "2026-1",
            courseIds = new List<Guid>
            {
                courses.Single(c => c.Code == "MAT-101").Id,
                courses.Single(c => c.Code == "FIS-102").Id,
                courses.Single(c => c.Code == "HUM-107").Id,
            }
        });

        res.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await res.Content.ReadFromJsonAsync<ErrorBody>(Json))!.Code.Should().Be("COURSE_PROFESSOR_CONFLICT");
    }

    [Fact]
    public async Task Enrollment_FourCourses_Returns422()
    {
        var (studentId, courses) = await SetupAsync("four");
        var res = await _client.PostAsJsonAsync("/api/enrollments", new
        {
            studentId,
            period = "2026-1",
            courseIds = ValidPick(courses).Concat(new[] { courses.Single(c => c.Code == "MAT-109").Id }).ToList()
        });

        res.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await res.Content.ReadFromJsonAsync<ErrorBody>(Json))!.Code.Should().Be("ENROLLMENT_COURSE_COUNT_INVALID");
    }

    [Fact]
    public async Task Enrollment_DuplicatePeriod_Returns409()
    {
        var (studentId, courses) = await SetupAsync("dup");
        var payload = new { studentId, period = "2026-1", courseIds = ValidPick(courses) };
        (await _client.PostAsJsonAsync("/api/enrollments", payload)).EnsureSuccessStatusCode();

        var again = await _client.PostAsJsonAsync("/api/enrollments", payload);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await again.Content.ReadFromJsonAsync<ErrorBody>(Json))!.Code.Should().Be("ENROLLMENT_DUPLICATED");
    }

    [Fact]
    public async Task Classmates_OnlyEnrolledRequester_CanSeeNames()
    {
        var (outsiderId, courses) = await SetupAsync("outsider");
        var (memberId, _) = await SetupAsync("member");
        var mat101 = courses.Single(c => c.Code == "MAT-101").Id;

        // Outsider (not enrolled in MAT-101) is forbidden.
        var forbidden = await _client.GetAsync(
            $"/api/enrollments/courses/{mat101}/classmates?studentId={outsiderId}");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await forbidden.Content.ReadFromJsonAsync<ErrorBody>(Json))!.Code.Should().Be("CLASSMATES_FORBIDDEN");

        // Enroll the member, then request as the member.
        var enroll = await _client.PostAsJsonAsync("/api/enrollments",
            new { studentId = memberId, period = "2026-1", courseIds = ValidPick(courses) });
        enroll.EnsureSuccessStatusCode();

        var ok = await _client.GetAsync(
            $"/api/enrollments/courses/{mat101}/classmates?studentId={memberId}&page=1&pageSize=20");
        ok.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await ok.Content.ReadAsStringAsync();
        raw.Should().Contain("fullName").And.NotContain("email");
    }

    [Fact]
    public async Task Classmates_MissingRequester_Returns400()
    {
        var courses = await _client.GetFromJsonAsync<Paged<CourseDto>>("/api/courses?page=1&pageSize=100", Json);
        var mat101 = courses!.Items.Single(c => c.Code == "MAT-101").Id;

        var res = await _client.GetAsync($"/api/enrollments/courses/{mat101}/classmates");
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await res.Content.ReadFromJsonAsync<ErrorBody>(Json))!.Code.Should().Be("VALIDATION_FAILED");
    }
}
