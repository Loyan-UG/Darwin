using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CMS.Media.DTOs;
using Darwin.Application.CMS.Media.Validators;
using Darwin.Domain.Entities.CMS;
using FluentValidation;

namespace Darwin.Application.CMS.Media.Commands
{
    /// <summary>
    /// Persists metadata for an already-uploaded media file. The actual file handling occurs in Web layer.
    /// </summary>
    public sealed class CreateMediaAssetHandler
    {
        private readonly IAppDbContext _db;
        private readonly MediaAssetCreateValidator _validator = new();

        public CreateMediaAssetHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(MediaAssetCreateDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            var entity = new MediaAsset
            {
                Url = dto.Url.Trim(),
                Alt = dto.Alt?.Trim() ?? string.Empty,
                Title = string.IsNullOrWhiteSpace(dto.Title) ? null : dto.Title.Trim(),
                OriginalFileName = dto.OriginalFileName.Trim(),
                SizeBytes = dto.SizeBytes,
                ContentHash = string.IsNullOrWhiteSpace(dto.ContentHash) ? null : dto.ContentHash.Trim(),
                Width = dto.Width,
                Height = dto.Height,
                Role = string.IsNullOrWhiteSpace(dto.Role) ? null : dto.Role.Trim()
            };

            _db.Set<MediaAsset>().Add(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
