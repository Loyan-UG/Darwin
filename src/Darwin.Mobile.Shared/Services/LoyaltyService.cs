using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Api;

namespace Darwin.Mobile.Shared.Services;

/// <summary>
/// Loyalty-facing operations: QR token, start scan, accrual/redeem, account summaries.
/// </summary>
public interface ILoyaltyService
{
    Task<QrCodePayloadDto> GetQrTokenAsync(CancellationToken ct);
    Task<StartScanResponse> StartScanAsync(StartScanRequest req, CancellationToken ct);
    Task<AddPointsResponse> AddPointsAsync(AddPointsRequest req, CancellationToken ct);
    Task<RedeemRewardResponse> RedeemAsync(RedeemRewardRequest req, CancellationToken ct);
    Task<IReadOnlyList<LoyaltyAccountSummary>> MyAccountsAsync(CancellationToken ct);
    Task<IReadOnlyList<PointsTransaction>> HistoryAsync(Guid businessId, int take, CancellationToken ct);
}

public sealed class LoyaltyService : ILoyaltyService
{
    private readonly IApiClient _api;
    public LoyaltyService(IApiClient api) => _api = api;

    public Task<QrCodePayloadDto> GetQrTokenAsync(CancellationToken ct)
        => _api.GetAsync<QrCodePayloadDto>("loyalty/qr", ct)!;

    public Task<StartScanResponse> StartScanAsync(StartScanRequest req, CancellationToken ct)
        => _api.PostAsync<StartScanRequest, StartScanResponse>("loyalty/scan/start", req, ct)!;

    public Task<AddPointsResponse> AddPointsAsync(AddPointsRequest req, CancellationToken ct)
        => _api.PostAsync<AddPointsRequest, AddPointsResponse>("loyalty/scan/add", req, ct)!;

    public Task<RedeemRewardResponse> RedeemAsync(RedeemRewardRequest req, CancellationToken ct)
        => _api.PostAsync<RedeemRewardRequest, RedeemRewardResponse>("loyalty/scan/redeem", req, ct)!;

    public Task<IReadOnlyList<LoyaltyAccountSummary>> MyAccountsAsync(CancellationToken ct)
        => _api.GetAsync<IReadOnlyList<LoyaltyAccountSummary>>("loyalty/me/accounts", ct)!;

    public Task<IReadOnlyList<PointsTransaction>> HistoryAsync(Guid businessId, int take, CancellationToken ct)
        => _api.GetAsync<IReadOnlyList<PointsTransaction>>($"loyalty/me/{businessId:D}/history?take={take}", ct)!;
}
