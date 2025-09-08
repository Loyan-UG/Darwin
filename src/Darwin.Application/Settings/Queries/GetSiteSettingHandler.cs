using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Settings.DTOs;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Settings.Queries
{
    public sealed class GetSiteSettingHandler
    {
        private readonly IAppDbContext _db;
        public GetSiteSettingHandler(IAppDbContext db) => _db = db;

        public async Task<SiteSettingDto?> HandleAsync(CancellationToken ct = default)
        {
            var s = await _db.Set<SiteSetting>().AsNoTracking().FirstOrDefaultAsync(ct);
            if (s == null) return null;
            return new SiteSettingDto
            {
                Id = s.Id,
                RowVersion = s.RowVersion,
                Title = s.Title,
                DefaultCulture = s.DefaultCulture,
                SupportedCulturesCsv = s.SupportedCulturesCsv
            };
        }
    }
}
