using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

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
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private const int MaxGalleryImages = 10;

        public CreateBusinessMediaHandler(
            IAppDbContext db,
            IValidator<BusinessMediaCreateDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Guid> HandleAsync(BusinessMediaCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var existing = await _db.Set<BusinessMedia>()
                .Where(x => x.BusinessId == dto.BusinessId && !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (existing.Count >= MaxGalleryImages)
            {
                throw new ValidationException(_localizer["BusinessMediaGalleryLimitExceeded"]);
            }

            var shouldBePrimary = dto.IsPrimary || existing.Count == 0;
            if (shouldBePrimary)
            {
                foreach (var item in existing)
                {
                    item.IsPrimary = false;
                }
            }

            var entity = new BusinessMedia
            {
                BusinessId = dto.BusinessId,
                BusinessLocationId = dto.BusinessLocationId,
                Url = dto.Url.Trim(),
                Caption = string.IsNullOrWhiteSpace(dto.Caption) ? null : dto.Caption.Trim(),
                SortOrder = dto.SortOrder <= 0 ? existing.Count + 1 : dto.SortOrder,
                IsPrimary = shouldBePrimary
            };

            _db.Set<BusinessMedia>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }
    }
}
