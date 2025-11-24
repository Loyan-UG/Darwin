using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Validators;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Processes a QR scan on a business device.
    /// Creates a <see cref="ScanSession"/> and, when accepted,
    /// either accrues points or redeems a reward depending on token purpose.
    /// </summary>
    public sealed class ProcessQrScanHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly ProcessQrScanValidator _validator = new();

        /// <summary>
        /// Initializes the handler.
        /// </summary>
        public ProcessQrScanHandler(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Processes a scan and returns the outcome for the business app.
        /// </summary>
        public async Task<Result<ProcessQrScanResultDto>> HandleAsync(ProcessQrScanDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid)
                return Result<ProcessQrScanResultDto>.Fail("Invalid scan request.");

            var now = _clock.UtcNow;

            var tokenEntity = await _db.Set<QrCodeToken>()
                .FirstOrDefaultAsync(x => x.Token == dto.Token, ct);

            if (tokenEntity is null)
            {
                return Result<ProcessQrScanResultDto>.Ok(new ProcessQrScanResultDto
                {
                    Accepted = false,
                    Outcome = "Rejected",
                    FailureReason = "Token not found."
                });
            }

            if (tokenEntity.ConsumedAtUtc is not null)
            {
                return Result<ProcessQrScanResultDto>.Ok(new ProcessQrScanResultDto
                {
                    Accepted = false,
                    Outcome = "Replayed",
                    FailureReason = "Token already consumed."
                });
            }

            if (tokenEntity.ExpiresAtUtc <= now)
            {
                return Result<ProcessQrScanResultDto>.Ok(new ProcessQrScanResultDto
                {
                    Accepted = false,
                    Outcome = "Expired",
                    FailureReason = "Token expired."
                });
            }

            if (tokenEntity.Purpose != dto.ExpectedPurpose)
            {
                return Result<ProcessQrScanResultDto>.Ok(new ProcessQrScanResultDto
                {
                    Accepted = false,
                    Outcome = "Rejected",
                    FailureReason = "Token purpose mismatch."
                });
            }

            tokenEntity.ConsumedAtUtc = now;
            tokenEntity.ConsumedByBusinessId = dto.BusinessId;
            tokenEntity.ConsumedByBusinessLocationId = dto.BusinessLocationId;

            var scanSession = new ScanSession
            {
                QrCodeTokenId = tokenEntity.Id,
                BusinessId = dto.BusinessId,
                BusinessLocationId = dto.BusinessLocationId,
                Outcome = "Accepted"
            };

            _db.Set<ScanSession>().Add(scanSession);

            LoyaltyAccount account;
            if (tokenEntity.LoyaltyAccountId.HasValue)
            {
                account = await _db.Set<LoyaltyAccount>()
                    .FirstOrDefaultAsync(a => a.Id == tokenEntity.LoyaltyAccountId.Value, ct)
                    ?? throw new ValidationException("Loyalty account not found.");
            }
            else
            {
                account = await _db.Set<LoyaltyAccount>()
                    .FirstOrDefaultAsync(a => a.BusinessId == dto.BusinessId && a.UserId == tokenEntity.UserId, ct)
                    ?? new LoyaltyAccount
                    {
                        BusinessId = dto.BusinessId,
                        UserId = tokenEntity.UserId,
                        Status = LoyaltyAccountStatus.Active
                    };

                if (account.Id == Guid.Empty)
                    _db.Set<LoyaltyAccount>().Add(account);
            }

            if (account.Status != LoyaltyAccountStatus.Active)
            {
                scanSession.Outcome = "Rejected";
                scanSession.FailureReason = "Account is not active.";

                await _db.SaveChangesAsync(ct);

                return Result<ProcessQrScanResultDto>.Ok(new ProcessQrScanResultDto
                {
                    Accepted = false,
                    Outcome = scanSession.Outcome,
                    FailureReason = scanSession.FailureReason,
                    LoyaltyAccountId = account.Id
                });
            }

            ProcessQrScanResultDto result;

            if (dto.ExpectedPurpose == QrTokenPurpose.Accrual)
            {
                var tx = new LoyaltyPointsTransaction
                {
                    LoyaltyAccountId = account.Id,
                    BusinessId = dto.BusinessId,
                    Type = LoyaltyPointsTransactionType.Accrual,
                    PointsDelta = 1,
                    BusinessLocationId = dto.BusinessLocationId,
                    PerformedByUserId = dto.PerformedByUserId
                };

                account.PointsBalance += 1;
                account.LifetimePoints += 1;
                account.LastAccrualAtUtc = now;

                _db.Set<LoyaltyPointsTransaction>().Add(tx);
                scanSession.ResultingTransactionId = tx.Id;

                result = new ProcessQrScanResultDto
                {
                    Accepted = true,
                    Outcome = "Accepted",
                    LoyaltyAccountId = account.Id,
                    NewPointsBalance = account.PointsBalance,
                    ResultingTransactionId = tx.Id
                };
            }
            else if (dto.ExpectedPurpose == QrTokenPurpose.Redemption)
            {
                if (!dto.LoyaltyRewardTierId.HasValue)
                    throw new ValidationException("Reward tier id is required for redemption.");

                var tier = await _db.Set<LoyaltyRewardTier>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == dto.LoyaltyRewardTierId.Value && !t.IsDeleted, ct);

                if (tier is null)
                    throw new ValidationException("Reward tier not found.");

                if (account.PointsBalance < tier.PointsRequired)
                {
                    scanSession.Outcome = "Rejected";
                    scanSession.FailureReason = "Insufficient points.";

                    await _db.SaveChangesAsync(ct);

                    return Result<ProcessQrScanResultDto>.Ok(new ProcessQrScanResultDto
                    {
                        Accepted = false,
                        Outcome = scanSession.Outcome,
                        FailureReason = scanSession.FailureReason,
                        LoyaltyAccountId = account.Id,
                        NewPointsBalance = account.PointsBalance
                    });
                }

                var redemption = new LoyaltyRewardRedemption
                {
                    LoyaltyAccountId = account.Id,
                    BusinessId = dto.BusinessId,
                    LoyaltyRewardTierId = tier.Id,
                    PointsSpent = tier.PointsRequired,
                    Status = LoyaltyRedemptionStatus.Confirmed,
                    BusinessLocationId = dto.BusinessLocationId
                };

                var tx = new LoyaltyPointsTransaction
                {
                    LoyaltyAccountId = account.Id,
                    BusinessId = dto.BusinessId,
                    Type = LoyaltyPointsTransactionType.Redemption,
                    PointsDelta = -tier.PointsRequired,
                    RewardRedemptionId = redemption.Id,
                    BusinessLocationId = dto.BusinessLocationId,
                    PerformedByUserId = dto.PerformedByUserId
                };

                account.PointsBalance -= tier.PointsRequired;

                _db.Set<LoyaltyRewardRedemption>().Add(redemption);
                _db.Set<LoyaltyPointsTransaction>().Add(tx);

                scanSession.ResultingTransactionId = tx.Id;

                result = new ProcessQrScanResultDto
                {
                    Accepted = true,
                    Outcome = "Accepted",
                    LoyaltyAccountId = account.Id,
                    NewPointsBalance = account.PointsBalance,
                    ResultingTransactionId = tx.Id,
                    RewardRedemptionId = redemption.Id
                };
            }
            else
            {
                result = new ProcessQrScanResultDto
                {
                    Accepted = true,
                    Outcome = "Accepted",
                    LoyaltyAccountId = account.Id,
                    NewPointsBalance = account.PointsBalance
                };
            }

            await _db.SaveChangesAsync(ct);
            return Result<ProcessQrScanResultDto>.Ok(result);
        }
    }
}
