using Darwin.Application.Loyalty.DTOs;
using FluentValidation;

namespace Darwin.Application.Loyalty.Validators
{
    /// <summary>
    /// Validates QR issuance requests.
    /// </summary>
    public sealed class IssueQrCodeTokenValidator : AbstractValidator<IssueQrCodeTokenDto>
    {
        public IssueQrCodeTokenValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.TtlSeconds).InclusiveBetween(30, 300);
            RuleFor(x => x.Purpose).IsInEnum();
            RuleFor(x => x.IssuedDeviceId).MaximumLength(200);
        }
    }

    /// <summary>
    /// Validates scan processing requests.
    /// </summary>
    public sealed class ProcessQrScanValidator : AbstractValidator<ProcessQrScanDto>
    {
        public ProcessQrScanValidator()
        {
            RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.ExpectedPurpose).IsInEnum();
        }
    }
}
