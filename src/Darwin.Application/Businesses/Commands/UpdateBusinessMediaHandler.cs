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
    /// Updates a <see cref="BusinessMedia"/> item with optimistic concurrency.
    /// </summary>
    public sealed class UpdateBusinessMediaHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<BusinessMediaEditDto> _validator;

        public UpdateBusinessMediaHandler(IAppDbContext db, IValidator<BusinessMediaEditDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(BusinessMediaEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await _db.Set<BusinessMedia>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (entity is null)
                throw new InvalidOperationException("Business media not found.");

            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            var requestVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(requestVersion))
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");

            entity.BusinessLocationId = dto.BusinessLocationId;
            entity.Url = dto.Url.Trim();
            entity.Caption = string.IsNullOrWhiteSpace(dto.Caption) ? null : dto.Caption.Trim();
            entity.SortOrder = dto.SortOrder;
            entity.IsPrimary = dto.IsPrimary;

            await _db.SaveChangesAsync(ct);
        }
    }
}
