using System;
using Darwin.Application.CMS.Media.DTOs;
using Darwin.Application.CMS.Media.Validators;
using FluentAssertions;

namespace Darwin.Tests.Unit.Media;

/// <summary>
/// Unit tests for <see cref="MediaAssetCreateValidator"/> and
/// <see cref="MediaAssetEditValidator"/>.
/// </summary>
public sealed class MediaAssetValidatorsTests
{
    // ─── MediaAssetCreateValidator ────────────────────────────────────────────

    [Fact]
    public void MediaAssetCreate_Should_Pass_For_Minimal_Valid_Dto()
    {
        var dto = new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/image.jpg",
            OriginalFileName = "image.jpg",
            SizeBytes = 102400
        };

        var result = new MediaAssetCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a well-formed asset create request should pass");
    }

    [Fact]
    public void MediaAssetCreate_Should_Pass_With_All_Optional_Fields()
    {
        var dto = new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/photo.png",
            OriginalFileName = "photo.png",
            Alt = "A beautiful landscape",
            Title = "Landscape Photo",
            SizeBytes = 204800,
            Width = 1920,
            Height = 1080,
            Role = "hero"
        };

        var result = new MediaAssetCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("all optional fields are within their allowed limits");
    }

    [Fact]
    public void MediaAssetCreate_Should_Fail_When_Url_Empty()
    {
        var dto = new MediaAssetCreateDto
        {
            Url = "",
            OriginalFileName = "image.jpg",
            SizeBytes = 1024
        };

        var result = new MediaAssetCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Url is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Url));
    }

    [Fact]
    public void MediaAssetCreate_Should_Fail_When_Url_Too_Long()
    {
        var dto = new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/" + new string('a', 2026),
            OriginalFileName = "image.jpg",
            SizeBytes = 1024
        };

        var result = new MediaAssetCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Url must not exceed 2048 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Url));
    }

    [Fact]
    public void MediaAssetCreate_Should_Fail_When_OriginalFileName_Empty()
    {
        var dto = new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/file.jpg",
            OriginalFileName = "",
            SizeBytes = 1024
        };

        var result = new MediaAssetCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("OriginalFileName is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.OriginalFileName));
    }

    [Fact]
    public void MediaAssetCreate_Should_Fail_When_OriginalFileName_Too_Long()
    {
        var dto = new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/file.jpg",
            OriginalFileName = new string('f', 513),
            SizeBytes = 1024
        };

        var result = new MediaAssetCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("OriginalFileName must not exceed 512 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.OriginalFileName));
    }

    [Fact]
    public void MediaAssetCreate_Should_Fail_When_Alt_Too_Long()
    {
        var dto = new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/img.jpg",
            OriginalFileName = "img.jpg",
            Alt = new string('A', 257),
            SizeBytes = 1024
        };

        var result = new MediaAssetCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Alt must not exceed 256 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Alt));
    }

    [Fact]
    public void MediaAssetCreate_Should_Fail_When_Title_Too_Long()
    {
        var dto = new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/img.jpg",
            OriginalFileName = "img.jpg",
            Title = new string('T', 257),
            SizeBytes = 1024
        };

        var result = new MediaAssetCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Title must not exceed 256 characters when provided");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Title));
    }

    [Fact]
    public void MediaAssetCreate_Should_Pass_When_Title_Is_Null()
    {
        var dto = new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/img.jpg",
            OriginalFileName = "img.jpg",
            Title = null,
            SizeBytes = 0
        };

        var result = new MediaAssetCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("Title is optional");
    }

    [Fact]
    public void MediaAssetCreate_Should_Fail_When_SizeBytes_Negative()
    {
        var dto = new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/img.jpg",
            OriginalFileName = "img.jpg",
            SizeBytes = -1
        };

        var result = new MediaAssetCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("SizeBytes must be >= 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SizeBytes));
    }

    [Fact]
    public void MediaAssetCreate_Should_Pass_When_SizeBytes_Is_Zero()
    {
        var dto = new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/img.jpg",
            OriginalFileName = "img.jpg",
            SizeBytes = 0
        };

        var result = new MediaAssetCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a size of 0 bytes is at the valid lower boundary");
    }

    [Fact]
    public void MediaAssetCreate_Should_Fail_When_Width_Is_Zero()
    {
        var dto = new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/img.jpg",
            OriginalFileName = "img.jpg",
            SizeBytes = 1024,
            Width = 0
        };

        var result = new MediaAssetCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Width must be > 0 when provided");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Width));
    }

    [Fact]
    public void MediaAssetCreate_Should_Fail_When_Height_Is_Zero()
    {
        var dto = new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/img.jpg",
            OriginalFileName = "img.jpg",
            SizeBytes = 1024,
            Height = 0
        };

        var result = new MediaAssetCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Height must be > 0 when provided");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Height));
    }

    [Fact]
    public void MediaAssetCreate_Should_Pass_When_Width_And_Height_Are_Null()
    {
        var dto = new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/img.jpg",
            OriginalFileName = "img.jpg",
            SizeBytes = 512,
            Width = null,
            Height = null
        };

        var result = new MediaAssetCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("Width and Height are optional");
    }

    [Fact]
    public void MediaAssetCreate_Should_Fail_When_Role_Too_Long()
    {
        var dto = new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/img.jpg",
            OriginalFileName = "img.jpg",
            SizeBytes = 1024,
            Role = new string('r', 65)
        };

        var result = new MediaAssetCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Role must not exceed 64 characters when provided");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Role));
    }

    [Fact]
    public void MediaAssetCreate_Should_Pass_When_Role_Is_Null()
    {
        var dto = new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/img.jpg",
            OriginalFileName = "img.jpg",
            SizeBytes = 1024,
            Role = null
        };

        var result = new MediaAssetCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("Role is optional");
    }

    // ─── MediaAssetEditValidator ──────────────────────────────────────────────

    [Fact]
    public void MediaAssetEdit_Should_Pass_For_Valid_Dto()
    {
        var dto = new MediaAssetEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Alt = "Updated alt text"
        };

        var result = new MediaAssetEditValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a fully valid edit DTO should pass");
    }

    [Fact]
    public void MediaAssetEdit_Should_Pass_With_All_Optional_Fields()
    {
        var dto = new MediaAssetEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Alt = "New alt",
            Title = "New Title",
            Role = "thumbnail"
        };

        var result = new MediaAssetEditValidator().Validate(dto);

        result.IsValid.Should().BeTrue("all optional fields within limits should pass");
    }

    [Fact]
    public void MediaAssetEdit_Should_Fail_When_Id_Is_Empty()
    {
        var dto = new MediaAssetEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 },
            Alt = "alt text"
        };

        var result = new MediaAssetEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Id must not be empty for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void MediaAssetEdit_Should_Fail_When_RowVersion_Is_Null()
    {
        var dto = new MediaAssetEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = null!,
            Alt = "alt text"
        };

        var result = new MediaAssetEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("RowVersion must not be null for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }

    [Fact]
    public void MediaAssetEdit_Should_Fail_When_Alt_Too_Long()
    {
        var dto = new MediaAssetEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Alt = new string('A', 257)
        };

        var result = new MediaAssetEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Alt must not exceed 256 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Alt));
    }

    [Fact]
    public void MediaAssetEdit_Should_Fail_When_Title_Too_Long()
    {
        var dto = new MediaAssetEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Title = new string('T', 257)
        };

        var result = new MediaAssetEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Title must not exceed 256 characters when provided");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Title));
    }

    [Fact]
    public void MediaAssetEdit_Should_Fail_When_Role_Too_Long()
    {
        var dto = new MediaAssetEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Role = new string('r', 65)
        };

        var result = new MediaAssetEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Role must not exceed 64 characters when provided");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Role));
    }

    [Fact]
    public void MediaAssetEdit_Should_Pass_When_All_Optional_Fields_Are_Null()
    {
        var dto = new MediaAssetEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Title = null,
            Role = null
        };

        var result = new MediaAssetEditValidator().Validate(dto);

        result.IsValid.Should().BeTrue("null optional fields are all allowed");
    }
}
