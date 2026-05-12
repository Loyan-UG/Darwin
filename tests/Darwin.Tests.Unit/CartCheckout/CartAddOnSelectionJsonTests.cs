using System;
using System.Collections.Generic;
using System.Text.Json;
using Darwin.Application.CartCheckout;
using FluentAssertions;

namespace Darwin.Tests.Unit.CartCheckout;

/// <summary>
/// Unit tests for <see cref="CartAddOnSelectionJson"/>.
/// Covers NormalizeIds and NormalizeJsonOrNull for all input branches including
/// null, empty, sorting, deduplication, invalid JSON, and round-trip consistency.
/// </summary>
public sealed class CartAddOnSelectionJsonTests
{
    // ─── NormalizeIds ─────────────────────────────────────────────────────────

    [Fact]
    public void NormalizeIds_Should_ReturnEmptyArrayJson_WhenIdsIsNull()
    {
        var result = CartAddOnSelectionJson.NormalizeIds(null);

        result.Should().Be("[]");
    }

    [Fact]
    public void NormalizeIds_Should_ReturnEmptyArrayJson_WhenIdsIsEmpty()
    {
        var result = CartAddOnSelectionJson.NormalizeIds(Array.Empty<Guid>());

        result.Should().Be("[]");
    }

    [Fact]
    public void NormalizeIds_Should_ProduceSameOutput_RegardlessOfInputOrder()
    {
        var id1 = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var id2 = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var id3 = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc");

        var forwardOrder = new[] { id1, id2, id3 };
        var reverseOrder = new[] { id3, id2, id1 };
        var shuffled = new[] { id2, id3, id1 };

        var resultForward = CartAddOnSelectionJson.NormalizeIds(forwardOrder);
        var resultReverse = CartAddOnSelectionJson.NormalizeIds(reverseOrder);
        var resultShuffled = CartAddOnSelectionJson.NormalizeIds(shuffled);

        resultForward.Should().Be(resultReverse);
        resultForward.Should().Be(resultShuffled);
    }

    [Fact]
    public void NormalizeIds_Should_DeduplicateGuids()
    {
        var id = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var duplicated = new[] { id, id, id };

        var result = CartAddOnSelectionJson.NormalizeIds(duplicated);

        var parsed = JsonSerializer.Deserialize<List<Guid>>(result);
        parsed.Should().HaveCount(1);
        parsed![0].Should().Be(id);
    }

    [Fact]
    public void NormalizeIds_Should_SortAndDeduplicateIds_WhenBothNeeded()
    {
        var id1 = new Guid("11111111-1111-1111-1111-111111111111");
        var id2 = new Guid("22222222-2222-2222-2222-222222222222");

        // Reversed + duplicated
        var input = new[] { id2, id1, id2, id1 };

        var result = CartAddOnSelectionJson.NormalizeIds(input);

        var parsed = JsonSerializer.Deserialize<List<Guid>>(result)!;
        parsed.Should().HaveCount(2);
        parsed[0].Should().Be(id1, "id1 sorts before id2");
        parsed[1].Should().Be(id2);
    }

    [Fact]
    public void NormalizeIds_Should_ProduceStableJson_ForSingleId()
    {
        var id = new Guid("deadbeef-dead-beef-dead-beefdeadbeef");

        var result = CartAddOnSelectionJson.NormalizeIds(new[] { id });

        var parsed = JsonSerializer.Deserialize<List<Guid>>(result)!;
        parsed.Should().HaveCount(1);
        parsed[0].Should().Be(id);
    }

    // ─── NormalizeJsonOrNull ─────────────────────────────────────────────────

    [Fact]
    public void NormalizeJsonOrNull_Should_ReturnNull_WhenInputIsNull()
    {
        var result = CartAddOnSelectionJson.NormalizeJsonOrNull(null);

        result.Should().BeNull();
    }

    [Fact]
    public void NormalizeJsonOrNull_Should_ReturnNull_WhenInputIsWhitespace()
    {
        var result = CartAddOnSelectionJson.NormalizeJsonOrNull("   ");

        result.Should().BeNull();
    }

    [Fact]
    public void NormalizeJsonOrNull_Should_ReturnSortedJson_WhenValidJsonArray()
    {
        var id1 = new Guid("11111111-1111-1111-1111-111111111111");
        var id2 = new Guid("22222222-2222-2222-2222-222222222222");

        // Input in reversed order
        var inputJson = JsonSerializer.Serialize(new[] { id2, id1 });

        var result = CartAddOnSelectionJson.NormalizeJsonOrNull(inputJson);

        result.Should().NotBeNull();
        var parsed = JsonSerializer.Deserialize<List<Guid>>(result!)!;
        parsed.Should().HaveCount(2);
        parsed[0].Should().Be(id1, "sorted order should be id1 then id2");
        parsed[1].Should().Be(id2);
    }

    [Fact]
    public void NormalizeJsonOrNull_Should_DeduplicateAndSort_WhenJsonContainsDuplicates()
    {
        var id = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var inputJson = JsonSerializer.Serialize(new[] { id, id });

        var result = CartAddOnSelectionJson.NormalizeJsonOrNull(inputJson);

        var parsed = JsonSerializer.Deserialize<List<Guid>>(result!)!;
        parsed.Should().HaveCount(1);
        parsed[0].Should().Be(id);
    }

    [Fact]
    public void NormalizeJsonOrNull_Should_ReturnTrimmedInput_WhenJsonIsInvalid()
    {
        const string invalidJson = "  not-valid-json  ";

        var result = CartAddOnSelectionJson.NormalizeJsonOrNull(invalidJson);

        result.Should().Be("not-valid-json");
    }

    [Fact]
    public void NormalizeJsonOrNull_Should_ReturnEmptyArrayJson_WhenValidEmptyArrayJson()
    {
        var result = CartAddOnSelectionJson.NormalizeJsonOrNull("[]");

        result.Should().Be("[]");
    }

    // ─── Round-trip consistency ───────────────────────────────────────────────

    [Fact]
    public void NormalizeIds_And_NormalizeJsonOrNull_Should_ProduceSameOutput_ForSameGuids()
    {
        var id1 = new Guid("aaaaaaaa-0000-0000-0000-aaaaaaaaaaaa");
        var id2 = new Guid("bbbbbbbb-0000-0000-0000-bbbbbbbbbbbb");

        var fromIds = CartAddOnSelectionJson.NormalizeIds(new[] { id2, id1 });
        var fromJson = CartAddOnSelectionJson.NormalizeJsonOrNull(
            JsonSerializer.Serialize(new[] { id1, id2 })); // different input order

        fromIds.Should().Be(fromJson,
            "both normalization paths should produce identical canonical JSON for the same set of GUIDs");
    }
}
