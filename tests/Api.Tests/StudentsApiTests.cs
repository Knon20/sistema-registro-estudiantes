using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Api.Tests;

[Collection("api")]
public sealed class StudentsApiTests
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public StudentsApiTests(ApiFixture fixture) => _client = fixture.Client;

    private static object NewStudent(Guid programId, string tag) => new
    {
        fullName = $"Test {tag}",
        email = $"test.{tag}.{Guid.NewGuid():N}@uni.edu",
        documentId = $"DOC-{tag}-{Guid.NewGuid():N}"[..20],
        programId
    };

    private async Task<Guid> ProgramIdAsync()
    {
        var page = await _client.GetFromJsonAsync<Paged<ProgramDto>>("/api/programs?page=1&pageSize=10", Json);
        return page!.Items[0].Id;
    }

    private sealed record ProgramDto(Guid Id, string Name, string Code);

    [Fact]
    public async Task Health_Returns200()
    {
        var res = await _client.GetAsync("/health");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Students_CRUD_Flow()
    {
        var programId = await ProgramIdAsync();

        var created = await (await _client.PostAsJsonAsync("/api/students", NewStudent(programId, "crud"))).Content
            .ReadFromJsonAsync<StudentDto>(Json);
        created.Should().NotBeNull();

        var got = await _client.GetFromJsonAsync<StudentDto>($"/api/students/{created!.Id}", Json);
        got!.Email.Should().Be(created.Email);

        var list = await _client.GetFromJsonAsync<Paged<StudentDto>>("/api/students?page=1&pageSize=20", Json);
        list!.Total.Should().BeGreaterThanOrEqualTo(1);
        list.Items.Should().Contain(s => s.Id == created.Id);

        var updateRes = await _client.PutAsJsonAsync($"/api/students/{created.Id}",
            new { fullName = "Test Crud Updated", email = created.Email, programId });
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteRes = await _client.DeleteAsync($"/api/students/{created.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var missing = await _client.GetAsync($"/api/students/{created.Id}");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var err = await missing.Content.ReadFromJsonAsync<ErrorBody>(Json);
        err!.Code.Should().Be("STUDENT_NOT_FOUND");
    }

    [Fact]
    public async Task Create_DuplicateEmail_Returns409()
    {
        var programId = await ProgramIdAsync();
        var email = $"dup.{Guid.NewGuid():N}@uni.edu";
        var payload = new { fullName = "Dup One", email, documentId = $"D1-{Guid.NewGuid():N}"[..20], programId };
        (await _client.PostAsJsonAsync("/api/students", payload)).EnsureSuccessStatusCode();

        var dup = await _client.PostAsJsonAsync("/api/students",
            new { fullName = "Dup Two", email, documentId = $"D2-{Guid.NewGuid():N}"[..20], programId });
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await dup.Content.ReadFromJsonAsync<ErrorBody>(Json))!.Code.Should().Be("STUDENT_DUPLICATED");
    }

    [Fact]
    public async Task Delete_Then_Recreate_Offers_Restore()
    {
        var programId = await ProgramIdAsync();
        var payload = new
        {
            fullName = "Restore Me",
            email = $"restore.{Guid.NewGuid():N}@uni.edu",
            documentId = $"R-{Guid.NewGuid():N}"[..20],
            programId
        };

        var created = await (await _client.PostAsJsonAsync("/api/students", payload)).Content
            .ReadFromJsonAsync<StudentDto>(Json);

        (await _client.DeleteAsync($"/api/students/{created!.Id}")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        var again = await _client.PostAsJsonAsync("/api/students", payload);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var err = await again.Content.ReadFromJsonAsync<ErrorBody>(Json);
        err!.Code.Should().Be("STUDENT_DELETED_EXISTS");

        var restoredRes = await _client.PostAsync($"/api/students/{created.Id}/restore", null);
        restoredRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var restored = await restoredRes.Content.ReadFromJsonAsync<StudentDto>(Json);
        restored!.Id.Should().Be(created.Id);
    }
}
