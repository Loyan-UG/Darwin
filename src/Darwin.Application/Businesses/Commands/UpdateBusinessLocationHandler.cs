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
    /// Updates an existing <see cref="BusinessLocation"/> with optimistic concurrency.
    /// </summary>
    public sealed class UpdateBusinessLocationHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<BusinessLocationEditDto> _validator;

        public UpdateBusinessLocationHandler(IAppDbContext db, IValidator<BusinessLocationEditDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(BusinessLocationEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await _db.Set<BusinessLocation>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (entity is null)
                throw new InvalidOperationException("Business location not found.");

            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            var requestVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(requestVersion))
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");

            entity.Name = dto.Name.Trim();
            entity.AddressLine1 = string.IsNullOrWhiteSpace(dto.AddressLine1) ? null : dto.AddressLine1.Trim();
            entity.AddressLine2 = string.IsNullOrWhiteSpace(dto.AddressLine2) ? null : dto.AddressLine2.Trim();
            entity.City = string.IsNullOrWhiteSpace(dto.City) ? null : dto.City.Trim();
            entity.Region = string.IsNullOrWhiteSpace(dto.Region) ? null : dto.Region.Trim();
            entity.CountryCode = string.IsNullOrWhiteSpace(dto.CountryCode) ? null : dto.CountryCode.Trim();
            entity.PostalCode = string.IsNullOrWhiteSpace(dto.PostalCode) ? null : dto.PostalCode.Trim();
            entity.Coordinate = dto.Coordinate == null
                ? null
                : new Darwin.Domain.Common.GeoCoordinate(dto.Coordinate.Latitude, dto.Coordinate.Longitude, dto.Coordinate.AltitudeMeters);
            entity.IsPrimary = dto.IsPrimary;
            entity.OpeningHoursJson = string.IsNullOrWhiteSpace(dto.OpeningHoursJson) ? null : dto.OpeningHoursJson.Trim();
            entity.InternalNote = string.IsNullOrWhiteSpace(dto.InternalNote) ? null : dto.InternalNote.Trim();

            await _db.SaveChangesAsync(ct);
        }
    }
}
