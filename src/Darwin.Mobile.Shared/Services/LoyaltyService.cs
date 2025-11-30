using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Api;

namespace Darwin.Mobile.Shared.Services;

/// <summary>
/// Provides a thin, testable abstraction over the loyalty-related Web API endpoints
/// that are shared between the consumer and business mobile applications.
/// </summary>
/// <remarks>
/// <para>
/// The API is fully session-based. A QR code never contains the internal user identifier
/// and instead encodes a scan session identifier created by the backend. The consumer app
/// prepares the session and shows the QR; the business app scans the QR and processes the
/// session (accrual or redemption).
/// </para>
/// <para>
/// This service does not impose any UI decisions; it only mirrors the contracts in
/// <see cref="Darwin.Contracts.Loyalty"/> and keeps the HTTP details hidden behind
/// <see cref="IApiClient"/>.
/// </para>
/// </remarks>
public interface ILoyaltyService
{
    /// <summary>
    /// Prepares a new scan session for the current customer and the specified business.
    /// </summary>
    /// <param name="request">
    /// The request describing the business, scan mode (accrual or redemption) and
    /// optionally the set of reward identifiers the customer would like to redeem.
    /// </param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    /// <returns>
    /// A task that resolves to the prepared scan session, including the session identifier
    /// to encode into the QR code and optional information about the selected rewards.
    /// </returns>
    Task<PrepareScanSessionResponse> PrepareScanSessionAsync(
        PrepareScanSessionRequest request,
        CancellationToken ct);

    /// <summary>
    /// Returns a lightweight summary of all loyalty accounts for the current customer,
    /// grouped by business.
    /// </summary>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    /// <returns>
    /// A task that resolves to a read-only list of <see cref="LoyaltyAccountSummary"/>
    /// instances, each describing a single loyalty relationship with a business
    /// (current points balance, last accrual timestamp and next reward hint).
    /// </returns>
    Task<IReadOnlyList<LoyaltyAccountSummary>> GetMyAccountsAsync(CancellationToken ct);

    /// <summary>
    /// Returns the points ledger for the current customer at the specified business.
    /// </summary>
    /// <param name="businessId">The identifier of the business whose history should be returned.</param>
    /// <param name="take">
    /// The maximum number of most recent transactions to retrieve. The backend can impose
    /// its own upper bound regardless of this value.
    /// </param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    /// <returns>
    /// A task that resolves to a read-only list of <see cref="PointsTransaction"/> entries
    /// ordered from newest to oldest.
    /// </returns>
    Task<IReadOnlyList<PointsTransaction>> GetHistoryAsync(
        Guid businessId,
        int take,
        CancellationToken ct);

    /// <summary>
    /// Processes a scan session on behalf of the business after a QR code has been scanned.
    /// </summary>
    /// <param name="request">
    /// The request containing the scan session identifier that was decoded from the QR code.
    /// </param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    /// <returns>
    /// A task that resolves to details about the scan session including its mode
    /// (accrual or redemption), the non-personal loyalty account summary and any
    /// rewards that were pre-selected by the customer, along with the set of
    /// allowed actions for the business.
    /// </returns>
    Task<ProcessScanSessionForBusinessResponse> ProcessScanSessionForBusinessAsync(
        ProcessScanSessionForBusinessRequest request,
        CancellationToken ct);

    /// <summary>
    /// Confirms an accrual (earning points) for the specified scan session.
    /// </summary>
    /// <param name="request">The request describing the scan session and the number of points to add.</param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    /// <returns>
    /// A task that resolves to a response indicating whether the accrual succeeded,
    /// the new points balance (if available) and optional error details.
    /// </returns>
    Task<ConfirmAccrualResponse> ConfirmAccrualAsync(
        ConfirmAccrualRequest request,
        CancellationToken ct);

    /// <summary>
    /// Confirms a redemption (spending points on rewards) for the specified scan session.
    /// </summary>
    /// <param name="request">The request describing the scan session to redeem.</param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    /// <returns>
    /// A task that resolves to a response indicating whether the redemption succeeded,
    /// the new points balance (if available) and optional error details.
    /// </returns>
    Task<ConfirmRedemptionResponse> ConfirmRedemptionAsync(
        ConfirmRedemptionRequest request,
        CancellationToken ct);
}

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
