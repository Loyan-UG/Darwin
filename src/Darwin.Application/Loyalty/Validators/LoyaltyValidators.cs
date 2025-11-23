using Darwin.Application.Loyalty.DTOs;
using FluentValidation;

namespace Darwin.Application.Loyalty.Validators
{
    /// <summary>
    /// Validator for issuing QR codes.
    /// Empty by design because issuing uses current user only.
    /// Kept for consistency and future extension.
    /// </summary>
    public sealed class IssueQrCodeValidator : AbstractValidator<object>
    {
        public IssueQrCodeValidator()
        {
        }
    }

    /// <summary>
    /// Validation rules for starting scan sessions.
    /// </summary>
    public sealed class StartScanValidator : AbstractValidator<StartScanRequestDto>
    {
        public StartScanValidator()
        {
            RuleFor(x => x.Token).NotEmpty().MinimumLength(8);
        }
    }

    /// <summary>
    /// Validation rules for accruing points.
    /// </summary>
    public sealed class AccruePointsValidator : AbstractValidator<AccruePointsRequestDto>
    {
        public AccruePointsValidator()
        {
            RuleFor(x => x.ScanSessionId).NotEmpty();
            RuleFor(x => x.PointsToAdd).GreaterThan(0).LessThanOrEqualTo(100_000);
        }
    }

    /// <summary>
    /// Validation rules for redeeming rewards.
    /// </summary>
    public sealed class RedeemRewardValidator : AbstractValidator<RedeemRewardRequestDto>
    {
        public RedeemRewardValidator()
        {
            RuleFor(x => x.ScanSessionId).NotEmpty();
            RuleFor(x => x.RewardTierId).NotEmpty();
        }
    }
}
