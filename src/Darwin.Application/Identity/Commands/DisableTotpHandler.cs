using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Disables TOTP for the given user and removes active secret(s).
    /// </summary>
    public sealed class DisableTotpHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public DisableTotpHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        /// <summary>
        /// Disables two-factor auth by removing the user's TOTP secrets and flag.
        /// </summary>
        public async Task<Result> HandleAsync(TotpDisableDto dto, CancellationToken ct = default)
        {
            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted, ct);
            if (user is null) return Result.Fail(_localizer["UserNotFound"]);

            var secrets = await _db.Set<UserTwoFactorSecret>()
                .Where(s => s.UserId == user.Id && !s.IsDeleted)
                .ToListAsync(ct);

            if (secrets.Count > 0)
                _db.Set<UserTwoFactorSecret>().RemoveRange(secrets);

            user.TwoFactorEnabled = false;

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
