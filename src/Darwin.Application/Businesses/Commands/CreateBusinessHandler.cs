using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Commands
{
    /// <summary>
    /// Creates a new <see cref="Business"/> aggregate root.
    /// Validation is enforced via FluentValidation, and strings are normalized.
    /// </summary>
    public sealed class CreateBusinessHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<BusinessCreateDto> _validator;

        /// <summary>
        /// Initializes the handler with persistence abstraction and validator.
        /// </summary>
        public CreateBusinessHandler(IAppDbContext db, IValidator<BusinessCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <summary>
        /// Creates the business and returns its identifier.
        /// </summary>
        public async Task<Guid> HandleAsync(BusinessCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);
            var settings = await _db.Set<SiteSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => !x.IsDeleted, ct)
                .ConfigureAwait(false) ?? new SiteSetting();
            var contactEmail = NormalizeNullable(dto.ContactEmail) ?? NormalizeNullable(settings.ContactEmail);
            var brandDisplayName = NormalizeNullable(dto.BrandDisplayName) ?? NormalizeNullable(settings.Title);
            var brandLogoUrl = NormalizeNullable(dto.BrandLogoUrl) ?? NormalizeNullable(settings.LogoUrl);
            var supportEmail = NormalizeNullable(dto.SupportEmail) ?? contactEmail ?? NormalizeNullable(settings.SmtpFromAddress);
            var communicationSenderName = NormalizeNullable(dto.CommunicationSenderName)
                                           ?? NormalizeNullable(settings.SmtpFromDisplayName)
                                           ?? brandDisplayName;
            var communicationReplyToEmail = NormalizeNullable(dto.CommunicationReplyToEmail)
                                            ?? contactEmail
                                            ?? NormalizeNullable(settings.SmtpFromAddress);

            var entity = new Business
            {
                Name = dto.Name.Trim(),
                LegalName = string.IsNullOrWhiteSpace(dto.LegalName) ? null : dto.LegalName.Trim(),
                TaxId = string.IsNullOrWhiteSpace(dto.TaxId) ? null : dto.TaxId.Trim(),
                ShortDescription = string.IsNullOrWhiteSpace(dto.ShortDescription) ? null : dto.ShortDescription.Trim(),
                WebsiteUrl = string.IsNullOrWhiteSpace(dto.WebsiteUrl) ? null : dto.WebsiteUrl.Trim(),
                ContactEmail = contactEmail,
                ContactPhoneE164 = string.IsNullOrWhiteSpace(dto.ContactPhoneE164) ? null : dto.ContactPhoneE164.Trim(),
                Category = dto.Category,
                DefaultCurrency = dto.DefaultCurrency.Trim(),
                DefaultCulture = dto.DefaultCulture.Trim(),
                DefaultTimeZoneId = dto.DefaultTimeZoneId.Trim(),
                AdminTextOverridesJson = string.IsNullOrWhiteSpace(dto.AdminTextOverridesJson) ? null : dto.AdminTextOverridesJson.Trim(),
                BrandDisplayName = brandDisplayName,
                BrandLogoUrl = brandLogoUrl,
                BrandPrimaryColorHex = string.IsNullOrWhiteSpace(dto.BrandPrimaryColorHex) ? null : dto.BrandPrimaryColorHex.Trim(),
                BrandSecondaryColorHex = string.IsNullOrWhiteSpace(dto.BrandSecondaryColorHex) ? null : dto.BrandSecondaryColorHex.Trim(),
                SupportEmail = supportEmail,
                CommunicationSenderName = communicationSenderName,
                CommunicationReplyToEmail = communicationReplyToEmail,
                CustomerEmailNotificationsEnabled = dto.CustomerEmailNotificationsEnabled,
                CustomerMarketingEmailsEnabled = dto.CustomerMarketingEmailsEnabled,
                OperationalAlertEmailsEnabled = dto.OperationalAlertEmailsEnabled,
                // New businesses remain inactive until an explicit approval action completes onboarding.
                IsActive = false,
                OperationalStatus = BusinessOperationalStatus.PendingApproval
            };

            _db.Set<Business>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
