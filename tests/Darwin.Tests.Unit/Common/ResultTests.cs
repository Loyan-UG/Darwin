using System.Collections.Generic;
using Darwin.Shared.Results;
using FluentAssertions;
using Xunit;

namespace Darwin.Tests.Unit.Common;

/// <summary>
/// Unit tests for the <see cref="Result"/>, <see cref="Result{T}"/>,
/// and <see cref="PagedResult{T}"/> lightweight result wrappers.
/// </summary>
public sealed class ResultTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Result (non-generic)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Result_Ok_Should_HaveSucceededTrue_And_NullError()
    {
        var result = Result.Ok();
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Result_Fail_Should_HaveSucceededFalse_And_ErrorMessage()
    {
        var result = Result.Fail("Something went wrong");
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Something went wrong");
    }

    [Fact]
    public void Result_Fail_Should_PreserveErrorMessage()
    {
        var message = "Entity not found";
        var result = Result.Fail(message);
        result.Error.Should().Be(message);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Result<T>
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ResultT_Ok_Should_HaveSucceededTrue_And_CarryValue()
    {
        var result = Result<string>.Ok("hello");
        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be("hello");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ResultT_Ok_Should_WorkForComplexTypes()
    {
        var obj = new { Id = 42, Name = "test" };
        var result = Result<object>.Ok(obj);
        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeSameAs(obj);
    }

    [Fact]
    public void ResultT_Fail_Should_HaveSucceededFalse_And_DefaultValue()
    {
        var result = Result<string>.Fail("not found");
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("not found");
        result.Value.Should().BeNull();
    }

    [Fact]
    public void ResultT_Fail_Should_ReturnDefaultForValueType()
    {
        var result = Result<int>.Fail("error");
        result.Succeeded.Should().BeFalse();
        result.Value.Should().Be(0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PagedResult<T>
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void PagedResult_Should_StoreAllProperties()
    {
        var items = new List<string> { "a", "b", "c" };
        var paged = new PagedResult<string>(items, totalCount: 100, pageNumber: 2, pageSize: 3);

        paged.Items.Should().BeEquivalentTo(items);
        paged.TotalCount.Should().Be(100);
        paged.PageNumber.Should().Be(2);
        paged.PageSize.Should().Be(3);
    }

    [Fact]
    public void PagedResult_Items_Should_BeReadOnly()
    {
        var items = new List<string> { "x" };
        var paged = new PagedResult<string>(items, 1, 1, 10);
        paged.Items.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    [Fact]
    public void PagedResult_Should_AllowEmptyItemList()
    {
        var paged = new PagedResult<int>(new List<int>(), 0, 1, 10);
        paged.Items.Should().BeEmpty();
        paged.TotalCount.Should().Be(0);
    }
}
