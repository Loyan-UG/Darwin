using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Commands
{
    /// <summary>
    /// Updates an existing <see cref="Business"/> with optimistic concurrency via RowVersion.
    /// </summary>
    public sealed class UpdateBusinessHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<BusinessEditDto> _validator;

        public UpdateBusinessHandler(IAppDbContext db, IValidator<BusinessEditDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(BusinessEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await _db.Set<Business>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (entity is null)
                throw new InvalidOperationException("Business not found.");

            // Concurrency check exactly like Brand.
            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            var requestVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(requestVersion))
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");

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
            entity.IsActive = dto.IsActive;

            await _db.SaveChangesAsync(ct);
        }
    }
}
