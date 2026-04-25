using System;
using Darwin.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Darwin.Tests.Unit.Common;

/// <summary>
/// Unit tests for the <see cref="GeoCoordinate"/> value object.
/// Covers construction validation, boundary values, property storage, and ToString.
/// </summary>
public sealed class GeoCoordinateTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Valid construction
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_Should_StoreValidCoordinates()
    {
        var coord = new GeoCoordinate(48.1351, 11.5820);
        coord.Latitude.Should().Be(48.1351);
        coord.Longitude.Should().Be(11.5820);
        coord.AltitudeMeters.Should().BeNull();
    }

    [Fact]
    public void Constructor_Should_StoreAltitudeWhenProvided()
    {
        var coord = new GeoCoordinate(52.52, 13.405, 35.0);
        coord.AltitudeMeters.Should().Be(35.0);
    }

    [Theory]
    [InlineData(-90.0)]
    [InlineData(0.0)]
    [InlineData(90.0)]
    public void Constructor_Should_AcceptBoundaryLatitudeValues(double latitude)
    {
        var coord = new GeoCoordinate(latitude, 0.0);
        coord.Latitude.Should().Be(latitude);
    }

    [Theory]
    [InlineData(-180.0)]
    [InlineData(0.0)]
    [InlineData(180.0)]
    public void Constructor_Should_AcceptBoundaryLongitudeValues(double longitude)
    {
        var coord = new GeoCoordinate(0.0, longitude);
        coord.Longitude.Should().Be(longitude);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Invalid construction
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(-90.001)]
    [InlineData(90.001)]
    [InlineData(-180.0)]
    [InlineData(180.0)]
    public void Constructor_Should_ThrowForLatitudeOutOfRange(double latitude)
    {
        Action act = () => _ = new GeoCoordinate(latitude, 0.0);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*[-90, 90]*");
    }

    [Theory]
    [InlineData(-180.001)]
    [InlineData(180.001)]
    public void Constructor_Should_ThrowForLongitudeOutOfRange(double longitude)
    {
        Action act = () => _ = new GeoCoordinate(0.0, longitude);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*[-180, 180]*");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ToString
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_Should_ReturnLatCommaLon()
    {
        var coord = new GeoCoordinate(48.1351, 11.582);
        coord.ToString().Should().Be("48.1351,11.582");
    }

    [Fact]
    public void ToString_Should_NotIncludeAltitude()
    {
        var coord = new GeoCoordinate(48.0, 11.0, 500.0);
        coord.ToString().Should().Be("48,11");
    }
}
