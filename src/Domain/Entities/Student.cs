using Domain.Common;

namespace Domain.Entities;

public sealed class Student : Entity
{
    public string FullName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string DocumentId { get; private set; } = default!;
    public Guid ProgramId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private Student() { }

    public Student(string fullName, string email, string documentId, Guid programId)
    {
        Rename(fullName);
        SetEmail(email);
        SetDocument(documentId);
        if (programId == Guid.Empty) throw new ArgumentException("Program is required.", nameof(programId));
        ProgramId = programId;
        CreatedAt = DateTime.UtcNow;
    }

    public void Rename(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length < 3)
            throw new ArgumentException("Student full name must have at least 3 characters.", nameof(fullName));
        FullName = fullName.Trim();
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("A valid email is required.", nameof(email));
        Email = email.Trim().ToLowerInvariant();
    }

    public void SetDocument(string documentId)
    {
        if (string.IsNullOrWhiteSpace(documentId))
            throw new ArgumentException("Document is required.", nameof(documentId));
        DocumentId = documentId.Trim();
    }

    public void ChangeProgram(Guid programId)
    {
        if (programId == Guid.Empty) throw new ArgumentException("Program is required.", nameof(programId));
        ProgramId = programId;
    }

    /// <summary>Soft delete: the record is kept for audit, hidden by a global query filter.</summary>
    public void MarkDeleted()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
