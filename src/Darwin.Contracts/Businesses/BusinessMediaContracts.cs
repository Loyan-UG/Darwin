using System;
using System.Collections.Generic;

namespace Darwin.Contracts.Businesses;

/// <summary>Business profile image and gallery payload.</summary>
public sealed class BusinessMediaLibrary
{
    public Guid BusinessId { get; init; }
    public string? ProfileImageUrl { get; init; }
    public IReadOnlyList<BusinessMediaItem> Gallery { get; init; } = Array.Empty<BusinessMediaItem>();
}

/// <summary>One business gallery image.</summary>
public sealed class BusinessMediaItem
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public Guid? BusinessLocationId { get; init; }
    public string Url { get; init; } = string.Empty;
    public string? Caption { get; init; }
    public int SortOrder { get; init; }
    public bool IsPrimary { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}

public sealed class SetBusinessProfileImageRequest
{
    public string? ProfileImageUrl { get; init; }
}

public sealed class CreateBusinessMediaRequest
{
    public Guid? BusinessLocationId { get; init; }
    public string Url { get; init; } = string.Empty;
    public string? Caption { get; init; }
    public int? SortOrder { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed class UpdateBusinessMediaRequest
{
    public Guid? BusinessLocationId { get; init; }
    public string Url { get; init; } = string.Empty;
    public string? Caption { get; init; }
    public int SortOrder { get; init; }
    public bool IsPrimary { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}

public sealed class DeleteBusinessMediaRequest
{
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}

public sealed class BusinessImageUploadResponse
{
    public string Url { get; init; } = string.Empty;
}
