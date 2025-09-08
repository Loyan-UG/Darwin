using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Settings.DTOs;
using Darwin.Application.Settings.Validators;
using Darwin.Domain.Entities.Settings;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Settings.Commands
{
    public sealed class UpdateSiteSettingHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<UpdateSiteSettingDto> _validator;

        public UpdateSiteSettingHandler(IAppDbContext db, IValidator<UpdateSiteSettingDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        public async Task HandleAsync(UpdateSiteSettingDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var s = await _db.Set<SiteSetting>().FirstOrDefaultAsync(ct);
            if (s == null)
                throw new ValidationException("SiteSetting row not found.");

            if (!s.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException("The settings were modified by another user.");

            s.Title = dto.Title.Trim();
            s.DefaultCulture = dto.DefaultCulture.Trim();
            s.SupportedCulturesCsv = string.Join(",",
                (dto.SupportedCulturesCsv ?? string.Empty)
                    .Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
                    .Distinct());

            await _db.SaveChangesAsync(ct);
        }
    }
}
