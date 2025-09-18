using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CMS.Media.DTOs;
using Darwin.Application.CMS.Media.Validators;
using Darwin.Domain.Entities.CMS;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CMS.Media.Commands
{
    /// <summary>
    /// Updates a media asset's descriptive fields (Alt/Title/Role) with optimistic concurrency.
    /// </summary>
    public sealed class UpdateMediaAssetHandler
    {
        private readonly IAppDbContext _db;
        private readonly MediaAssetEditValidator _validator = new();

        public UpdateMediaAssetHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(MediaAssetEditDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            var entity = await _db.Set<MediaAsset>().FirstOrDefaultAsync(m => m.Id == dto.Id && !m.IsDeleted, ct);
            if (entity is null) throw new InvalidOperationException("Media asset not found.");

            if (!entity.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");

            entity.Alt = dto.Alt?.Trim() ?? string.Empty;
            entity.Title = string.IsNullOrWhiteSpace(dto.Title) ? null : dto.Title.Trim();
            entity.Role = string.IsNullOrWhiteSpace(dto.Role) ? null : dto.Role.Trim();

            await _db.SaveChangesAsync(ct);
        }
    }
}
