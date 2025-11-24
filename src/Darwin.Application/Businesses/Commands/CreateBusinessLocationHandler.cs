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
    /// Creates a new <see cref="BusinessLocation"/> for a business.
    /// </summary>
    public sealed class CreateBusinessLocationHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<BusinessLocationCreateDto> _validator;

        public CreateBusinessLocationHandler(IAppDbContext db, IValidator<BusinessLocationCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Guid> HandleAsync(BusinessLocationCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = new BusinessLocation
            {
                BusinessId = dto.BusinessId,
                Name = dto.Name.Trim(),
                AddressLine1 = string.IsNullOrWhiteSpace(dto.AddressLine1) ? null : dto.AddressLine1.Trim(),
                AddressLine2 = string.IsNullOrWhiteSpace(dto.AddressLine2) ? null : dto.AddressLine2.Trim(),
                City = string.IsNullOrWhiteSpace(dto.City) ? null : dto.City.Trim(),
                Region = string.IsNullOrWhiteSpace(dto.Region) ? null : dto.Region.Trim(),
                CountryCode = string.IsNullOrWhiteSpace(dto.CountryCode) ? null : dto.CountryCode.Trim(),
                PostalCode = string.IsNullOrWhiteSpace(dto.PostalCode) ? null : dto.PostalCode.Trim(),
                Coordinate = dto.Coordinate == null
                    ? null
                    : new Darwin.Domain.Common.GeoCoordinate(dto.Coordinate.Latitude, dto.Coordinate.Longitude, dto.Coordinate.AltitudeMeters),
                IsPrimary = dto.IsPrimary,
                OpeningHoursJson = string.IsNullOrWhiteSpace(dto.OpeningHoursJson) ? null : dto.OpeningHoursJson.Trim(),
                InternalNote = string.IsNullOrWhiteSpace(dto.InternalNote) ? null : dto.InternalNote.Trim()
            };

            _db.Set<BusinessLocation>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }
    }
}
