using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Pricing.DTOs;
using Darwin.Application.Pricing.Validators;
using Darwin.Domain.Entities.Pricing;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Pricing.Commands
{
    /// <summary>
    /// Updates an existing promotion; checks concurrency via RowVersion and code uniqueness among active promotions.
    /// </summary>
    public sealed class UpdatePromotionHandler
    {
        private readonly IAppDbContext _db;
        private readonly PromotionEditValidator _validator = new();

        public UpdatePromotionHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(PromotionEditDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            var entity = await _db.Set<Promotion>().FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (entity is null) throw new InvalidOperationException("Promotion not found.");

            if (!entity.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");

            if (!string.IsNullOrWhiteSpace(dto.Code))
            {
                var exists = await _db.Set<Promotion>().AsNoTracking()
                    .AnyAsync(p => p.Id != dto.Id && p.IsActive && p.Code != null && p.Code.ToLower() == dto.Code.ToLower(), ct);
                if (exists)
                    throw new ValidationException("Coupon code must be unique among active promotions.");
            }

            entity.Name = dto.Name.Trim();
            entity.Code = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim();
            entity.Type = dto.Type;
            entity.AmountMinor = dto.AmountMinor;
            entity.Percent = dto.Percent;
            entity.Currency = dto.Currency.Trim();
            entity.MinSubtotalNetMinor = dto.MinSubtotalNetMinor;
            entity.ConditionsJson = dto.ConditionsJson;
            entity.StartsAtUtc = dto.StartsAtUtc;
            entity.EndsAtUtc = dto.EndsAtUtc;
            entity.MaxRedemptions = dto.MaxRedemptions;
            entity.PerCustomerLimit = dto.PerCustomerLimit;
            entity.IsActive = dto.IsActive;

            await _db.SaveChangesAsync(ct);
        }
    }
}
