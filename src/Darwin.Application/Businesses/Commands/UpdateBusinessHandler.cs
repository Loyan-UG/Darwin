using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Commands
{
    /// <summary>
    /// Updates an existing <see cref="Business"/> with optimistic concurrency via RowVersion.
    /// </summary>
    public sealed class UpdateBusinessHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<BusinessEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateBusinessHandler(
            IAppDbContext db,
            IValidator<BusinessEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(BusinessEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await _db.Set<Business>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct);

            if (entity is null)
                throw new InvalidOperationException(_localizer["BusinessNotFound"]);

            // Concurrency check exactly like Brand.
            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            var requestVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (requestVersion.Length == 0 || !currentVersion.SequenceEqual(requestVersion))
                throw new ValidationException(_localizer["ConcurrencyConflictDetected"]);

            entity.Name = dto.Name.Trim();
            entity.LegalName = string.IsNullOrWhiteSpace(dto.LegalName) ? null : dto.LegalName.Trim();
            entity.TaxId = string.IsNullOrWhiteSpace(dto.TaxId) ? null : dto.TaxId.Trim();
            entity.ShortDescription = string.IsNullOrWhiteSpace(dto.ShortDescription) ? null : dto.ShortDescription.Trim();
            entity.WebsiteUrl = string.IsNullOrWhiteSpace(dto.WebsiteUrl) ? null : dto.WebsiteUrl.Trim();
            entity.ContactEmail = string.IsNullOrWhiteSpace(dto.ContactEmail) ? null : dto.ContactEmail.Trim();
            entity.ContactPhoneE164 = string.IsNullOrWhiteSpace(dto.ContactPhoneE164) ? null : dto.ContactPhoneE164.Trim();
            entity.Category = dto.Category;
            entity.DefaultCurrency = dto.DefaultCurrency.Trim();
            entity.DefaultCulture = dto.DefaultCulture.Trim();
            entity.DefaultTimeZoneId = dto.DefaultTimeZoneId.Trim();
            entity.AdminTextOverridesJson = string.IsNullOrWhiteSpace(dto.AdminTextOverridesJson) ? null : dto.AdminTextOverridesJson.Trim();
            entity.BrandDisplayName = string.IsNullOrWhiteSpace(dto.BrandDisplayName) ? null : dto.BrandDisplayName.Trim();
            entity.BrandLogoUrl = string.IsNullOrWhiteSpace(dto.BrandLogoUrl) ? null : dto.BrandLogoUrl.Trim();
            entity.BrandPrimaryColorHex = string.IsNullOrWhiteSpace(dto.BrandPrimaryColorHex) ? null : dto.BrandPrimaryColorHex.Trim();
            entity.BrandSecondaryColorHex = string.IsNullOrWhiteSpace(dto.BrandSecondaryColorHex) ? null : dto.BrandSecondaryColorHex.Trim();
            entity.SupportEmail = string.IsNullOrWhiteSpace(dto.SupportEmail) ? null : dto.SupportEmail.Trim();
            entity.CommunicationSenderName = string.IsNullOrWhiteSpace(dto.CommunicationSenderName) ? null : dto.CommunicationSenderName.Trim();
            entity.CommunicationReplyToEmail = string.IsNullOrWhiteSpace(dto.CommunicationReplyToEmail) ? null : dto.CommunicationReplyToEmail.Trim();
            entity.CustomerEmailNotificationsEnabled = dto.CustomerEmailNotificationsEnabled;
            entity.CustomerMarketingEmailsEnabled = dto.CustomerMarketingEmailsEnabled;
            entity.OperationalAlertEmailsEnabled = dto.OperationalAlertEmailsEnabled;
            entity.IsActive = entity.OperationalStatus == BusinessOperationalStatus.Approved && dto.IsActive;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ValidationException(_localizer["ConcurrencyConflictDetected"]);
            }
        }
    }
}
