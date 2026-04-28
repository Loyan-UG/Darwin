using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Settings.DTOs;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Settings.Queries
{
    /// <summary>
    /// Returns supported cultures and default culture from SiteSetting.
    /// </summary>
    public sealed class GetCulturesHandler
    {
        private readonly IAppDbContext _db;
        public GetCulturesHandler(IAppDbContext db) => _db = db;

        public async Task<(string DefaultCulture, string[] Cultures)> HandleAsync(CancellationToken ct = default)
        {
            var setting = await _db.Set<SiteSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => !x.IsDeleted, ct);
            if (setting == null)
                return (
                    SiteSettingDto.DefaultCultureDefault,
                    SiteSettingDto.SupportedCulturesCsvDefault
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Prepend(SiteSettingDto.DefaultCultureDefault)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray());

            var def = string.IsNullOrWhiteSpace(setting.DefaultCulture)
                ? SiteSettingDto.DefaultCultureDefault
                : setting.DefaultCulture.Trim();

            var supportedCulturesCsv = string.IsNullOrWhiteSpace(setting.SupportedCulturesCsv)
                ? SiteSettingDto.SupportedCulturesCsvDefault
                : setting.SupportedCulturesCsv;

            var list = supportedCulturesCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Prepend(def)
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (list.Length == 0)
                list = SiteSettingDto.SupportedCulturesCsvDefault
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Prepend(def)
                    .Where(static x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            return (def, list);
        }
    }
}
