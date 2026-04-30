using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Loyalty.Commands
{
    public sealed class ExpireLoyaltyScanSessionDto
    {
        public Guid Id { get; init; }
        public Guid BusinessId { get; init; }
        public byte[] RowVersion { get; init; } = Array.Empty<byte>();
    }

    public sealed class ExpireExpiredLoyaltyScanSessionsDto
    {
        public Guid BusinessId { get; init; }
    }

    public sealed class ExpireExpiredLoyaltyScanSessionsResultDto
    {
        public int ExpiredCount { get; init; }
    }

    public sealed class ExpireLoyaltyScanSessionHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public ExpireLoyaltyScanSessionHandler(IAppDbContext db, IClock clock, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(ExpireLoyaltyScanSessionDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty || dto.BusinessId == Guid.Empty)
            {
                return Result.Fail(_localizer["LoyaltyScanSessionRequired"]);
            }

            var entity = await _db.Set<ScanSession>()
                .SingleOrDefaultAsync(x => !x.IsDeleted && x.Id == dto.Id && x.BusinessId == dto.BusinessId, ct)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return Result.Fail(_localizer["LoyaltyScanSessionNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                return Result.Fail(_localizer["LoyaltyScanSessionConcurrencyConflict"]);
            }

            var nowUtc = _clock.UtcNow;
            if (entity.Status != LoyaltyScanStatus.Pending || entity.ExpiresAtUtc > nowUtc)
            {
                return Result.Fail(_localizer["LoyaltyScanSessionCannotExpire"]);
            }

            Expire(entity);

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return Result.Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["LoyaltyScanSessionConcurrencyConflict"]);
            }
        }

        public async Task<Result<ExpireExpiredLoyaltyScanSessionsResultDto>> HandleExpiredAsync(ExpireExpiredLoyaltyScanSessionsDto dto, CancellationToken ct = default)
        {
            if (dto.BusinessId == Guid.Empty)
            {
                return Result<ExpireExpiredLoyaltyScanSessionsResultDto>.Fail(_localizer["BusinessIdRequired"]);
            }

            var nowUtc = _clock.UtcNow;
            var sessions = await _db.Set<ScanSession>()
                .Where(x => !x.IsDeleted && x.BusinessId == dto.BusinessId && x.Status == LoyaltyScanStatus.Pending && x.ExpiresAtUtc <= nowUtc)
                .OrderBy(x => x.ExpiresAtUtc)
                .Take(200)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var session in sessions)
            {
                Expire(session);
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return Result<ExpireExpiredLoyaltyScanSessionsResultDto>.Ok(new ExpireExpiredLoyaltyScanSessionsResultDto
                {
                    ExpiredCount = sessions.Count
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<ExpireExpiredLoyaltyScanSessionsResultDto>.Fail(_localizer["LoyaltyScanSessionConcurrencyConflict"]);
            }
        }

        private static void Expire(ScanSession session)
        {
            session.Status = LoyaltyScanStatus.Expired;
            session.Outcome = "Expired";
            session.FailureReason = "Expired";
        }
    }
}
