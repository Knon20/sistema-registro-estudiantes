namespace Application.Common;

/// <summary>Normalized pagination input. Single place for page/pageSize rules.</summary>
public sealed record PageRequest(int Page, int PageSize)
{
    public const int MaxPageSize = 100;

    public static PageRequest Normalize(int page, int pageSize) =>
        new(Math.Max(1, page), Math.Clamp(pageSize, 1, MaxPageSize));

    public int Skip => (Page - 1) * PageSize;
}

/// <summary>Infrastructure-level slice (entities), mapped to DTO PagedResult at the edge.</summary>
public sealed record PagedSlice<T>(IReadOnlyList<T> Items, int Total);
