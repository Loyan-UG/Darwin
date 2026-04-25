using Darwin.Application.Orders.StateMachine;
using Darwin.Domain.Enums;
using FluentAssertions;

namespace Darwin.Tests.Unit.Orders;

/// <summary>
/// Unit tests for <see cref="OrderStatePolicy"/> so that the allowed and
/// forbidden status transitions remain deterministic and auditable.
/// </summary>
public sealed class OrderStatePolicyTests
{
    private readonly OrderStatePolicy _policy = new();

    // ─── Allowed transitions ─────────────────────────────────────────────────

    [Theory]
    [InlineData(OrderStatus.Created, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Created, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Paid)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Paid, OrderStatus.PartiallyShipped)]
    [InlineData(OrderStatus.Paid, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Paid, OrderStatus.Refunded)]
    [InlineData(OrderStatus.Paid, OrderStatus.PartiallyRefunded)]
    [InlineData(OrderStatus.PartiallyShipped, OrderStatus.Shipped)]
    [InlineData(OrderStatus.PartiallyShipped, OrderStatus.Delivered)]
    [InlineData(OrderStatus.PartiallyShipped, OrderStatus.PartiallyRefunded)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Shipped, OrderStatus.PartiallyRefunded)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Refunded)]
    [InlineData(OrderStatus.Delivered, OrderStatus.PartiallyRefunded)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Refunded)]
    [InlineData(OrderStatus.PartiallyRefunded, OrderStatus.Refunded)]
    public void IsAllowed_Should_Return_True_For_Valid_Transition(OrderStatus from, OrderStatus to)
    {
        _policy.IsAllowed(from, to).Should().BeTrue($"{from} → {to} is a defined allowed transition");
    }

    // ─── Forbidden transitions ───────────────────────────────────────────────

    [Theory]
    [InlineData(OrderStatus.Created, OrderStatus.Paid)]
    [InlineData(OrderStatus.Created, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Created, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Created, OrderStatus.Refunded)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.PartiallyShipped)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Refunded)]
    [InlineData(OrderStatus.Paid, OrderStatus.Created)]
    [InlineData(OrderStatus.Paid, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Paid, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Created)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Shipped)]
    public void IsAllowed_Should_Return_False_For_Invalid_Transition(OrderStatus from, OrderStatus to)
    {
        _policy.IsAllowed(from, to).Should().BeFalse($"{from} → {to} is not a defined allowed transition");
    }

    // ─── Terminal states ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Refunded)]
    public void IsAllowed_Should_Return_False_For_Any_Transition_From_Terminal_State(OrderStatus terminalStatus)
    {
        var allStatuses = Enum.GetValues<OrderStatus>();

        foreach (var target in allStatuses)
        {
            _policy.IsAllowed(terminalStatus, target)
                .Should().BeFalse($"{terminalStatus} is terminal and no further transitions should be allowed");
        }
    }

    // ─── Self-transitions ────────────────────────────────────────────────────

    [Theory]
    [InlineData(OrderStatus.Created)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.PartiallyShipped)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.PartiallyRefunded)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Refunded)]
    public void IsAllowed_Should_Return_False_For_Self_Transition(OrderStatus status)
    {
        _policy.IsAllowed(status, status)
            .Should().BeFalse("self-transitions are not meaningful and should not be permitted");
    }
}
