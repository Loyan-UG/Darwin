using Darwin.Contracts.Identity;
using Darwin.Contracts.Invoices;
using Darwin.Contracts.Orders;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Cart;
using Darwin.Contracts.Catalog;
using Darwin.Contracts.Cms;
using Darwin.Contracts.Loyalty;
using Darwin.Contracts.Profile;
using Darwin.Contracts.Shipping;
using FluentAssertions;
using System.Text.Json;

namespace Darwin.Tests.Unit.Contracts;

/// <summary>
///     Shared serializer settings for contract-compatibility tests.
/// </summary>
public abstract class ContractSerializationCompatibilityTestBase
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
