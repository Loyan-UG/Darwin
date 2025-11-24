using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using FluentValidation;

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

            var entity = new Business
            {
                Name = dto.Name.Trim(),
                LegalName = string.IsNullOrWhiteSpace(dto.LegalName) ? null : dto.LegalName.Trim(),
                TaxId = string.IsNullOrWhiteSpace(dto.TaxId) ? null : dto.TaxId.Trim(),
                ShortDescription = string.IsNullOrWhiteSpace(dto.ShortDescription) ? null : dto.ShortDescription.Trim(),
                WebsiteUrl = string.IsNullOrWhiteSpace(dto.WebsiteUrl) ? null : dto.WebsiteUrl.Trim(),
                ContactEmail = string.IsNullOrWhiteSpace(dto.ContactEmail) ? null : dto.ContactEmail.Trim(),
                ContactPhoneE164 = string.IsNullOrWhiteSpace(dto.ContactPhoneE164) ? null : dto.ContactPhoneE164.Trim(),
                Category = dto.Category,
                DefaultCurrency = dto.DefaultCurrency.Trim(),
                DefaultCulture = dto.DefaultCulture.Trim(),
                IsActive = dto.IsActive
            };

            _db.Set<Business>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }
    }
}
