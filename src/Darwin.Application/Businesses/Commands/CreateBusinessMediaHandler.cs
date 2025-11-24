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
    /// Creates a new <see cref="BusinessMedia"/> item.
    /// This entity is logic-managed; hard delete is allowed.
    /// </summary>
    public sealed class CreateBusinessMediaHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<BusinessMediaCreateDto> _validator;

        public CreateBusinessMediaHandler(IAppDbContext db, IValidator<BusinessMediaCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Guid> HandleAsync(BusinessMediaCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = new BusinessMedia
            {
                BusinessId = dto.BusinessId,
                BusinessLocationId = dto.BusinessLocationId,
                Url = dto.Url.Trim(),
                Caption = string.IsNullOrWhiteSpace(dto.Caption) ? null : dto.Caption.Trim(),
                SortOrder = dto.SortOrder,
                IsPrimary = dto.IsPrimary
            };

            _db.Set<BusinessMedia>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }
    }
}
