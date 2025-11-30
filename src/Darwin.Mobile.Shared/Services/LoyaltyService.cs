using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Api;

namespace Darwin.Mobile.Shared.Services;

/// <summary>
/// Default implementation of <see cref="ILoyaltyService"/> that uses <see cref="IApiClient"/>
/// to communicate with the Darwin Web API.
/// </summary>
public sealed class LoyaltyService : ILoyaltyService
{
    private readonly IApiClient _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoyaltyService"/> class.
    /// </summary>
    /// <param name="api">
    /// The low-level HTTP abstraction used to send requests to the backend.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="api"/> is <c>null</c>.
    /// </exception>
    public LoyaltyService(IApiClient api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <inheritdoc />
    public Task<PrepareScanSessionResponse> PrepareScanSessionAsync(
        PrepareScanSessionRequest request,
        CancellationToken ct)
        => _api.PostAsync<PrepareScanSessionRequest, PrepareScanSessionResponse>(
            "loyalty/scan-session/prepare",
            request,
            ct)!;

    /// <inheritdoc />
    public Task<IReadOnlyList<LoyaltyAccountSummary>> GetMyAccountsAsync(CancellationToken ct)
        => _api.GetAsync<IReadOnlyList<LoyaltyAccountSummary>>(
            "loyalty/me/accounts",
            ct)!;

    /// <inheritdoc />
    public Task<IReadOnlyList<PointsTransaction>> GetHistoryAsync(
        Guid businessId,
        int take,
        CancellationToken ct)
        => _api.GetAsync<IReadOnlyList<PointsTransaction>>(
            $"loyalty/me/{businessId:D}/history?take={take}",
            ct)!;

    /// <inheritdoc />
    public Task<ProcessScanSessionForBusinessResponse> ProcessScanSessionForBusinessAsync(
        ProcessScanSessionForBusinessRequest request,
        CancellationToken ct)
        => _api.PostAsync<ProcessScanSessionForBusinessRequest, ProcessScanSessionForBusinessResponse>(
            "loyalty/scan-session/business/process",
            request,
            ct)!;

    /// <inheritdoc />
    public Task<ConfirmAccrualResponse> ConfirmAccrualAsync(
        ConfirmAccrualRequest request,
        CancellationToken ct)
        => _api.PostAsync<ConfirmAccrualRequest, ConfirmAccrualResponse>(
            "loyalty/scan-session/business/confirm-accrual",
            request,
            ct)!;

    /// <inheritdoc />
    public Task<ConfirmRedemptionResponse> ConfirmRedemptionAsync(
        ConfirmRedemptionRequest request,
        CancellationToken ct)
        => _api.PostAsync<ConfirmRedemptionRequest, ConfirmRedemptionResponse>(
            "loyalty/scan-session/business/confirm-redemption",
            request,
            ct)!;
}
