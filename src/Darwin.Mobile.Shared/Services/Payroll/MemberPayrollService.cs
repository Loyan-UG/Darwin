using Darwin.Contracts.Common;
using Darwin.Contracts.HumanResources;
using Darwin.Mobile.Shared.Api;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Services.Payroll;

/// <summary>
/// Member-facing payroll self-service calls. Payslip data is not locally cached by default.
/// </summary>
public sealed class MemberPayrollService : IMemberPayrollService
{
    private readonly IApiClient _apiClient;

    public MemberPayrollService(IApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public async Task<Result<PagedResponse<MemberPayslipSummary>>> GetMyPayslipsAsync(int page, int pageSize, CancellationToken ct)
    {
        if (page <= 0)
        {
            return Result<PagedResponse<MemberPayslipSummary>>.Fail("Page must be a positive integer.");
        }

        if (pageSize <= 0 || pageSize > 200)
        {
            return Result<PagedResponse<MemberPayslipSummary>>.Fail("PageSize must be between 1 and 200.");
        }

        var route = $"{ApiRoutes.Payroll.GetMyPayslips}?page={page}&pageSize={pageSize}";
        var response = await _apiClient.GetResultAsync<PagedResponse<MemberPayslipSummary>>(route, ct).ConfigureAwait(false);
        if (!response.Succeeded || response.Value is null)
        {
            return Result<PagedResponse<MemberPayslipSummary>>.Fail(response.Error ?? "Request failed while retrieving payslip history.");
        }

        return Result<PagedResponse<MemberPayslipSummary>>.Ok(response.Value);
    }

    public async Task<Result<ApiFileResult>> DownloadPayslipDocumentAsync(Guid payslipId, CancellationToken ct)
    {
        if (payslipId == Guid.Empty)
        {
            return Result<ApiFileResult>.Fail("PayslipId is required.");
        }

        var response = await _apiClient.GetFileResultAsync(ApiRoutes.Payroll.DownloadPayslipDocument(payslipId), ct).ConfigureAwait(false);
        if (!response.Succeeded || response.Value is null)
        {
            return Result<ApiFileResult>.Fail(response.Error ?? "Request failed while downloading payslip document.");
        }

        return Result<ApiFileResult>.Ok(response.Value);
    }
}
