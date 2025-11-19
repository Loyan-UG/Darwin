namespace Darwin.Contracts.Common;

/// <summary>Single sort option (e.g., "Name:asc").</summary>
public sealed class SortOption
{
    /// <summary>Field name to sort by.</summary>
    public string Field { get; init; } = "Name";

    /// <summary>Sort direction: "asc" or "desc".</summary>
    public string Direction { get; init; } = "asc";
}
