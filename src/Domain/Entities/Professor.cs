using Domain.Common;

namespace Domain.Entities;

public sealed class Professor : Entity
{
    public string FullName { get; private set; } = default!;
    public string Email { get; private set; } = default!;

    private readonly List<Course> _courses = new();
    public IReadOnlyCollection<Course> Courses => _courses.AsReadOnly();

    private Professor() { }

    public Professor(string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Professor name is required.", nameof(fullName));
        FullName = fullName.Trim();
        Email = email?.Trim() ?? string.Empty;
    }
}
