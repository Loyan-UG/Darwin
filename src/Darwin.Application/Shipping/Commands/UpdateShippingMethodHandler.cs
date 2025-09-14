using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Shipping.DTOs;
using Darwin.Domain.Entities.Shipping;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Shipping.Commands
{
    /// <summary>
    ///     Use‑case handler that updates an existing shipping method. It loads
    ///     the aggregate, validates the DTO, applies changes including rate
    ///     replacements, checks for concurrency conflicts via RowVersion and
    ///     persists the modified aggregate.
    /// </summary>
    public sealed class UpdateShippingMethodHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<ShippingMethodEditDto> _validator;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateShippingMethodHandler"/>.
        /// We inject only the DbContext and the validator. AutoMapper is not used here;
        /// mapping from DTO to the domain entity is performed manually to keep the
        /// mapping logic explicit and free from hidden conventions.
        /// </summary>
        /// <param name="db">Database context used to load and persist entities.</param>
        /// <param name="validator">FluentValidation validator for <see cref="ShippingMethodEditDto"/>.</param>
        public UpdateShippingMethodHandler(IAppDbContext db, IValidator<ShippingMethodEditDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        /// <summary>
        /// Updates an existing shipping method based on the supplied DTO. Throws
        /// when the method does not exist or when a concurrency conflict is
        /// detected.
        /// </summary>
        /// <param name="dto">Edited values including rates and RowVersion.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <exception cref="ValidationException">Thrown when validation fails.</exception>
        /// <exception cref="DbUpdateConcurrencyException">Thrown on version mismatch.</exception>
        public async Task HandleAsync(ShippingMethodEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await _db.Set<ShippingMethod>()
                .Include(sm => sm.Rates)
                .FirstOrDefaultAsync(sm => sm.Id == dto.Id, ct);

            if (entity == null)
            {
                throw new ValidationException("Shipping method not found.");
            }

            // Concurrency check
            if (!entity.RowVersion.SequenceEqual(dto.RowVersion))
            {
                throw new DbUpdateConcurrencyException("Shipping method was modified by another process.");
            }

            // Map simple properties
            entity.Name = dto.Name;
            entity.Carrier = dto.Carrier;
            entity.Service = dto.Service;
            entity.CountriesCsv = dto.CountriesCsv;
            entity.IsActive = dto.IsActive;

            // Replace rates collection. We clear the existing collection and then
            // instantiate new rate entities for each DTO. We avoid modifying
            // existing entities because EF Core would track them and apply
            // unintended modifications.
            entity.Rates.Clear();
            foreach (var r in dto.Rates)
            {
                var rate = new ShippingRate
                {
                    MaxWeight = r.MaxWeight,
                    MaxSubtotalNetMinor = r.MaxSubtotalNetMinor,
                    PriceMinor = r.PriceMinor,
                    SortOrder = r.SortOrder
                };
                entity.Rates.Add(rate);
            }

            // Save changes; EF Core will update RowVersion automatically (if configured)
            await _db.SaveChangesAsync(ct);
        }
    }
}