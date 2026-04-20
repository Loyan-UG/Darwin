using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Queries
{
    /// <summary>
    /// Minimal query to retrieve the current security stamp for a given user.
    /// This is used by the Web layer after secondary authentication steps (e.g., TOTP/WebAuthn)
    /// to issue the authentication cookie without direct DbContext access from Web.
    /// </summary>
    public sealed class GetSecurityStampHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        /// <summary>Creates a new instance of the handler.</summary>
        public GetSecurityStampHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Loads the user's security stamp if the user exists and is active.
        /// </summary>
        public async Task<Result<string>> HandleAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _db.Set<User>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted && u.IsActive, ct);

            if (user is null || string.IsNullOrWhiteSpace(user.SecurityStamp))
                return Result<string>.Fail(_localizer["UserNotFoundOrInactive"]);

            return Result<string>.Ok(user.SecurityStamp);
        }
    }
}
