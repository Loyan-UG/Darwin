using System;

namespace Darwin.Contracts.Loyalty;

/// <summary>
/// QR payload displayed by consumer app. NEVER includes internal user id.
/// It contains a short-lived, opaque token issued by the server.
/// </summary>
public sealed class QrCodePayloadDto
{
    /// <summary>Format version for forward compatibility (e.g., "LOY1").</summary>
    public string Version { get; init; } = "LOY1";

    /// <summary>Opaque, short-lived token bound to the user.</summary>
    public string Token { get; init; } = default!;
}

/// <summary>
/// Initiates a scan on business tablet by submitting customer's QR token.
/// API resolves the customer and returns a scan session to proceed.
/// </summary>
public sealed class StartScanRequest
{
    public string Token { get; init; } = default!;

    /// <summary>Optional: current location id of the business device.</summary>
    public Guid? BusinessLocationId { get; init; }
}

/// <summary>Business-facing view of the identified customer at scan time.</summary>
public sealed class StartScanResponse
{
    /// <summary>Ephemeral session id to be used for accrual/redemption.</summary>
    public Guid ScanSessionId { get; init; }

    /// <summary>Customer display name for the cashier to confirm verbally.</summary>
    public string CustomerDisplayName { get; init; } = default!;

    /// <summary>Current points at this business.</summary>
    public int CurrentPoints { get; init; }

    /// <summary>Next reward preview (if any).</summary>
    public string? NextRewardTitle { get; init; }
}

/// <summary>Adds points for the ongoing scan session.</summary>
public sealed class AddPointsRequest
{
    public Guid ScanSessionId { get; init; }

    /// <summary>Default +1 for per-visit mode.</summary>
    public int Points { get; init; } = 1;

    /// <summary>Optional reason/reference for audit.</summary>
    public string? Note { get; init; }
}

public sealed class AddPointsResponse
{
    public int NewBalance { get; init; }
    public DateTime AccruedAtUtc { get; init; }
}

/// <summary>Redeems a reward tier within an active scan session.</summary>
public sealed class RedeemRewardRequest
{
    public Guid ScanSessionId { get; init; }
    public Guid RewardTierId { get; init; }
}

public sealed class RedeemRewardResponse
{
    public int PointsSpent { get; init; }
    public int NewBalance { get; init; }
    public string RewardTitle { get; init; } = default!;
}

/// <summary>Consumer-facing summary for "My Rewards" list.</summary>
public sealed class LoyaltyAccountSummaryDto
{
    public Guid BusinessId { get; init; }
    public string BusinessName { get; init; } = default!;
    public int PointsBalance { get; init; }
    public DateTime? LastAccrualAtUtc { get; init; }
    public string? NextRewardTitle { get; init; }
}

/// <summary>History entry for points ledger.</summary>
public sealed class PointsTransactionDto
{
    public DateTime OccurredAtUtc { get; init; }
    public string Type { get; init; } = "Accrual"; // Accrual/Redemption/Adjustment
    public int Delta { get; init; }
    public string? Reference { get; init; }
    public string? Notes { get; init; }
}

/// <summary>Program overview displayed on business profile.</summary>
public sealed class LoyaltyProgramSummaryDto
{
    public Guid LoyaltyProgramId { get; init; }
    public string Name { get; init; } = default!;
    public string AccrualMode { get; init; } = "PerVisit";
    public IReadOnlyList<LoyaltyRewardTierDto> RewardTiers { get; init; } = Array.Empty<LoyaltyRewardTierDto>();
}

public sealed class LoyaltyRewardTierDto
{
    public Guid LoyaltyRewardTierId { get; init; }
    public int Threshold { get; init; }
    public string Title { get; init; } = default!;
    public string? Description { get; init; }
}
