using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

using Darwin.Application.Settings.DTOs;

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
            var setting = await _db.Set<SiteSetting>().AsNoTracking().FirstOrDefaultAsync(ct);
            if (setting == null)
                return (
                    SiteSettingDto.DefaultCultureDefault,
                    SiteSettingDto.SupportedCulturesCsvDefault
                        .Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries));

            var list = (setting.SupportedCulturesCsv ?? "")
                .Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
                .Distinct()
                .ToArray();

            if (list.Length == 0)
                list = (setting.SupportedCulturesCsv ?? SiteSettingDto.SupportedCulturesCsvDefault)
                    .Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
                    .Distinct()
                    .ToArray();

            var def = string.IsNullOrWhiteSpace(setting.DefaultCulture) ? SiteSettingDto.DefaultCultureDefault : setting.DefaultCulture;
            return (def, list);
        }
    }
}
