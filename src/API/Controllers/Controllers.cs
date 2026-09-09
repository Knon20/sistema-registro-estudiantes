using Application.DTOs;
using Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/students")]
public sealed class StudentsController : ControllerBase
{
    private readonly StudentService _service;
    public StudentsController(StudentService service) => _service = service;

    [HttpPost]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateStudentRequest req, CancellationToken ct)
    {
        var created = await _service.CreateAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await _service.GetAsync(id, ct);
        return Ok(dto);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StudentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await _service.ListAsync(ct);
        return Ok(items);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentRequest req, CancellationToken ct)
    {
        var dto = await _service.UpdateAsync(id, req, ct);
        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api")]
public sealed class CatalogController : ControllerBase
{
    private readonly CatalogService _catalog;
    public CatalogController(CatalogService catalog) => _catalog = catalog;

    [HttpGet("programs")]
    public async Task<IActionResult> Programs(CancellationToken ct) => Ok(await _catalog.ProgramsAsync(ct));

    [HttpGet("professors")]
    public async Task<IActionResult> Professors(CancellationToken ct) => Ok(await _catalog.ProfessorsAsync(ct));

    [HttpGet("courses")]
    public async Task<IActionResult> Courses(CancellationToken ct) => Ok(await _catalog.CoursesAsync(ct));
}

[ApiController]
[Route("api/enrollments")]
public sealed class EnrollmentsController : ControllerBase
{
    private readonly EnrollmentService _service;
    public EnrollmentsController(EnrollmentService service) => _service = service;

    [HttpPost]
    [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateEnrollmentRequest req, CancellationToken ct)
    {
        var dto = await _service.CreateAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { studentId = dto.StudentId, period = dto.Period }, dto);
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<IActionResult> Get(Guid studentId, [FromQuery] string period = "2026-1", CancellationToken ct = default)
    {
        var dto = await _service.GetAsync(studentId, period, ct);
        return dto is null ? NotFound(new { code = "ENROLLMENT_NOT_FOUND", message = "Enrollment not found." }) : Ok(dto);
    }

    [HttpPut("student/{studentId:guid}")]
    public async Task<IActionResult> Update(Guid studentId, [FromQuery] string period = "2026-1", [FromBody] UpdateEnrollmentRequest? req = null, CancellationToken ct = default)
    {
        if (req is null) return BadRequest(new { code = "VALIDATION_FAILED", message = "CourseIds are required." });
        var dto = await _service.UpdateAsync(studentId, period, req, ct);
        return Ok(dto);
    }

    /// <summary>Returns ONLY the names of classmates in a course.</summary>
    [HttpGet("courses/{courseId:guid}/classmates")]
    public async Task<IActionResult> Classmates(Guid courseId, CancellationToken ct)
    {
        var items = await _service.ClassmatesAsync(courseId, ct);
        return Ok(items);
    }
}
