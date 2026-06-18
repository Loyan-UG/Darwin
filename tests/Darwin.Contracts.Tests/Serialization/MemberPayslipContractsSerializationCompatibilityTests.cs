using System.Text.Json;
using Darwin.Contracts.HumanResources;
using FluentAssertions;

namespace Darwin.Contracts.Tests.Serialization;

public sealed class MemberPayslipContractsSerializationCompatibilityTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    [Fact]
    public void MemberPayslipSummary_Should_RoundTripWithEmployeeVisibleFields()
    {
        var summary = new MemberPayslipSummary
        {
            Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            BusinessId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            BusinessName = "Darwin Demo",
            EmployeeId = Guid.Parse("99999999-8888-7777-6666-555555555555"),
            EmployeeNumber = "EMP-001",
            PayslipNumber = "PS-2026-06-001",
            Currency = "EUR",
            PeriodStartUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEndUtc = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            GrossPayMinor = 420000,
            EmployeeDeductionMinor = 125000,
            NetPayMinor = 295000,
            Status = "Generated",
            PaymentStatus = "Paid",
            GeneratedAtUtc = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc),
            DocumentPath = "api/v1/member/payroll/payslips/11111111-2222-3333-4444-555555555555/document"
        };

        var json = JsonSerializer.Serialize(summary, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<MemberPayslipSummary>(json, JsonOptions);

        roundTrip.Should().NotBeNull();
        roundTrip!.Id.Should().Be(summary.Id);
        roundTrip.BusinessId.Should().Be(summary.BusinessId);
        roundTrip.EmployeeId.Should().Be(summary.EmployeeId);
        roundTrip.PayslipNumber.Should().Be(summary.PayslipNumber);
        roundTrip.PaymentStatus.Should().Be("Paid");
        roundTrip.DocumentPath.Should().Be(summary.DocumentPath);
        json.Contains("journal", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        json.Contains("bankReconciliation", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        json.Contains("provider", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        json.Contains("html", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
    }
}
