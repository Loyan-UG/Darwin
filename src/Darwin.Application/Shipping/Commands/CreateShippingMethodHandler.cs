using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Shipping.DTOs;
using Darwin.Domain.Entities.Shipping;
using FluentValidation;

namespace Darwin.Application.Shipping.Commands
{
    /// <summary>
    ///     Use‑case handler that creates a new shipping method along with its
    ///     tiered rates. The handler performs validation, maps the DTO into
    ///     domain entities, and persists the aggregate in a single unit of work.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Responsibilities:
    ///         <list type="bullet">
    ///             <item>Validate input via FluentValidation (including unique
    ///                  name constraints).</item>
    ///             <item>Map the DTO to domain entities using AutoMapper.</item>
    ///             <item>Persist the aggregate using the application DbContext.</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public sealed class CreateShippingMethodHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<ShippingMethodCreateDto> _validator;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateShippingMethodHandler"/>.
        /// We inject only the DbContext and the validator. No AutoMapper is used here;
        /// instead, the handler maps DTO properties to the domain entity explicitly.
        /// Explicit mapping reduces coupling to AutoMapper configuration and makes
        /// the mapping logic easier to follow and maintain.
        /// </summary>
        /// <param name="db">Database context used to persist entities.</param>
        /// <param name="validator">FluentValidation validator for <see cref="ShippingMethodCreateDto"/>.</param>
        public CreateShippingMethodHandler(IAppDbContext db, IValidator<ShippingMethodCreateDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        /// <summary>
        /// Handles the creation request asynchronously.
        /// </summary>
        /// <param name="dto">Input DTO with method properties and rate definitions.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Identifier of the newly created shipping method.</returns>
        /// <exception cref="ValidationException">Thrown when validation fails.</exception>
        public async Task<Guid> HandleAsync(ShippingMethodCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            // Manually map the DTO into a new domain entity. We avoid using AutoMapper
            // here because there is no mapping profile configured for ShippingMethod and
            // explicit mapping clarifies which properties are populated.
            var entity = new ShippingMethod
            {
                Name = dto.Name,
                Carrier = dto.Carrier,
                Service = dto.Service,
                CountriesCsv = dto.CountriesCsv,
                IsActive = dto.IsActive
            };

            // Map each rate DTO into a ShippingRate domain object. We do not
            // reuse existing rate entities because this is a new aggregate.
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

            // Add and persist the aggregate. EF Core will generate Id and RowVersion.
            _db.Set<ShippingMethod>().Add(entity);
            await _db.SaveChangesAsync(ct);

            return entity.Id;
        }
    }
}