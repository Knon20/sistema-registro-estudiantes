namespace Domain.ValueObjects;

public sealed record EnrollmentPeriod
{
    public string Value { get; }

    public EnrollmentPeriod(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Period is required.", nameof(value));
        Value = value.Trim();
    }

    public static implicit operator string(EnrollmentPeriod p) => p.Value;
    public override string ToString() => Value;
}
