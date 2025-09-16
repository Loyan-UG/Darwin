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
    /// Creates a new promotion and enforces unique active code (case-insensitive).
    /// </summary>
    public sealed class CreatePromotionHandler
    {
        private readonly IAppDbContext _db;
        private readonly PromotionCreateValidator _validator = new();

        public CreatePromotionHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(PromotionCreateDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            if (!string.IsNullOrWhiteSpace(dto.Code))
            {
                var exists = await _db.Set<Promotion>().AsNoTracking()
                    .AnyAsync(p => p.IsActive && p.Code != null && p.Code.ToLower() == dto.Code.ToLower(), ct);
                if (exists)
                    throw new ValidationException("Coupon code must be unique among active promotions.");
            }

            var entity = new Promotion
            {
                Name = dto.Name.Trim(),
                Code = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim(),
                Type = dto.Type,
                AmountMinor = dto.AmountMinor,
                Percent = dto.Percent,
                Currency = dto.Currency.Trim(),
                MinSubtotalNetMinor = dto.MinSubtotalNetMinor,
                ConditionsJson = dto.ConditionsJson,
                StartsAtUtc = dto.StartsAtUtc,
                EndsAtUtc = dto.EndsAtUtc,
                MaxRedemptions = dto.MaxRedemptions,
                PerCustomerLimit = dto.PerCustomerLimit,
                IsActive = dto.IsActive
            };

            _db.Set<Promotion>().Add(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
