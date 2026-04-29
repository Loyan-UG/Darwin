using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CMS.Media;
using Darwin.Application.CMS.Media.DTOs;
using Darwin.Application.CMS.Media.Validators;
using Darwin.Domain.Entities.CMS;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.CMS.Media.Commands
{
    /// <summary>
    /// Updates a media asset's descriptive fields (Alt/Title/Role) with optimistic concurrency.
    /// </summary>
    public sealed class UpdateMediaAssetHandler
    {
        private readonly IAppDbContext _db;
        private readonly MediaAssetEditValidator _validator = new();
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateMediaAssetHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task HandleAsync(MediaAssetEditDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            var entity = await _db.Set<MediaAsset>().FirstOrDefaultAsync(m => m.Id == dto.Id && !m.IsDeleted, ct);
            if (entity is null) throw new InvalidOperationException(_localizer["MediaAssetNotFound"]);

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0)
                throw new ValidationException(_localizer["RowVersionRequired"]);

            var currentRowVersion = entity.RowVersion ?? Array.Empty<byte>();
            if (!currentRowVersion.SequenceEqual(rowVersion))
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);

            entity.Alt = dto.Alt?.Trim() ?? string.Empty;
            entity.Title = string.IsNullOrWhiteSpace(dto.Title) ? null : dto.Title.Trim();
            entity.Role = MediaAssetRoleConventions.NormalizeRole(dto.Role);

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }
        }

    }
}
