using Domain.Common;

namespace Domain.Entities;

public sealed class AcademicProgram : Entity
{
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;

    private AcademicProgram() { }

    public AcademicProgram(string name, string code)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Program name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Program code is required.", nameof(code));
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
    }
}
