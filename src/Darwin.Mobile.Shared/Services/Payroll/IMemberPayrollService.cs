using Darwin.Contracts.Common;
using Darwin.Contracts.HumanResources;
using Darwin.Mobile.Shared.Api;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Services.Payroll;

/// <summary>
/// Employee self-service payroll service for member-scoped payslip history and official PDF download.
/// </summary>
public interface IMemberPayrollService
{
    Task<Result<PagedResponse<MemberPayslipSummary>>> GetMyPayslipsAsync(int page, int pageSize, CancellationToken ct);
    Task<Result<ApiFileResult>> DownloadPayslipDocumentAsync(Guid payslipId, CancellationToken ct);
}
