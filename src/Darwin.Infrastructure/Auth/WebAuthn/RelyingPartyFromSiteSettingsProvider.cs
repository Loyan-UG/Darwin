using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Auth.WebAuthn
{
    /// <summary>
    /// Reads WebAuthn relying party settings from SiteSettings row.
    /// Assumes there's a single settings record. Adjust if you have multi-tenant settings.
    /// </summary>
    public interface IRelyingPartyFromSiteSettingsProvider
    {
        Task<(string RpId, string RpName, string[] Origins, bool RequireUserVerification)> GetAsync(CancellationToken ct);
    }

    internal sealed class RelyingPartyFromSiteSettingsProvider : IRelyingPartyFromSiteSettingsProvider
    {
        private readonly IAppDbContext _db;
        public RelyingPartyFromSiteSettingsProvider(IAppDbContext db) => _db = db;

        public async Task<(string RpId, string RpName, string[] Origins, bool RequireUserVerification)> GetAsync(CancellationToken ct)
        {
            var row = await _db.Set<Darwin.Domain.Entities.Settings.SiteSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(ct) ?? new Darwin.Domain.Entities.Settings.SiteSetting();

            var csv = row.WebAuthnAllowedOriginsCsv ?? "https://localhost:5001";
            var origins = csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(x => x.Trim())
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .ToArray();

            var rpId = row.WebAuthnRelyingPartyId ?? "localhost";
            var rpName = row.WebAuthnRelyingPartyName ?? "Darwin";
            var require = row.WebAuthnRequireUserVerification;

            return (rpId, rpName, origins, require);
        }
    }
}
