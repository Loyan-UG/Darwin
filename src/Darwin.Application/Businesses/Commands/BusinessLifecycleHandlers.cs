using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Commands
{
    /// <summary>
    /// Approves a business for operational use and clears any suspension markers.
    /// </summary>
    public sealed class ApproveBusinessHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IValidator<BusinessLifecycleActionDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public ApproveBusinessHandler(
            IAppDbContext db,
            IClock clock,
            IValidator<BusinessLifecycleActionDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(BusinessLifecycleActionDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await LoadBusinessAsync(dto, ct);
            if (entity.OperationalStatus != BusinessOperationalStatus.PendingApproval)
            {
                throw new ValidationException(_localizer["BusinessLifecycleUnsupportedAction"]);
            }

            await BusinessApprovalPrerequisites.ValidateAsync(_db, entity, _localizer, ct).ConfigureAwait(false);

            entity.OperationalStatus = BusinessOperationalStatus.Approved;
            entity.ApprovedAtUtc ??= _clock.UtcNow;
            entity.SuspendedAtUtc = null;
            entity.SuspensionReason = null;
            entity.IsActive = true;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ValidationException(_localizer["ConcurrencyConflictDetected"]);
            }
        }

        private async Task<Business> LoadBusinessAsync(BusinessLifecycleActionDto dto, CancellationToken ct)
        {
            var entity = await _db.Set<Business>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct);
            if (entity is null)
            {
                throw new InvalidOperationException(_localizer["BusinessNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new ValidationException(_localizer["ConcurrencyConflictDetected"]);
            }

            return entity;
        }

    }

    /// <summary>
    /// Suspends a business and records an optional operator note.
    /// </summary>
    public sealed class SuspendBusinessHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IValidator<BusinessLifecycleActionDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public SuspendBusinessHandler(
            IAppDbContext db,
            IClock clock,
            IValidator<BusinessLifecycleActionDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(BusinessLifecycleActionDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await LoadBusinessAsync(dto, ct);
            if (entity.OperationalStatus != BusinessOperationalStatus.Approved)
            {
                throw new ValidationException(_localizer["BusinessLifecycleUnsupportedAction"]);
            }

            entity.OperationalStatus = BusinessOperationalStatus.Suspended;
            entity.SuspendedAtUtc = _clock.UtcNow;
            entity.SuspensionReason = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
            entity.IsActive = false;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ValidationException(_localizer["ConcurrencyConflictDetected"]);
            }
        }

        private async Task<Business> LoadBusinessAsync(BusinessLifecycleActionDto dto, CancellationToken ct)
        {
            var entity = await _db.Set<Business>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct);
            if (entity is null)
            {
                throw new InvalidOperationException(_localizer["BusinessNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new ValidationException(_localizer["ConcurrencyConflictDetected"]);
            }

            return entity;
        }
    }

    /// <summary>
    /// Reactivates a previously suspended business without changing its structural onboarding data.
    /// </summary>
    public sealed class ReactivateBusinessHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IValidator<BusinessLifecycleActionDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public ReactivateBusinessHandler(
            IAppDbContext db,
            IClock clock,
            IValidator<BusinessLifecycleActionDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(BusinessLifecycleActionDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await LoadBusinessAsync(dto, ct);
            if (entity.OperationalStatus != BusinessOperationalStatus.Suspended)
            {
                throw new ValidationException(_localizer["BusinessLifecycleUnsupportedAction"]);
            }

            await BusinessApprovalPrerequisites.ValidateAsync(_db, entity, _localizer, ct).ConfigureAwait(false);

            entity.OperationalStatus = BusinessOperationalStatus.Approved;
            entity.ApprovedAtUtc ??= _clock.UtcNow;
            entity.SuspendedAtUtc = null;
            entity.SuspensionReason = null;
            entity.IsActive = true;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ValidationException(_localizer["ConcurrencyConflictDetected"]);
            }
        }

        private async Task<Business> LoadBusinessAsync(BusinessLifecycleActionDto dto, CancellationToken ct)
        {
            var entity = await _db.Set<Business>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct);
            if (entity is null)
            {
                throw new InvalidOperationException(_localizer["BusinessNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new ValidationException(_localizer["ConcurrencyConflictDetected"]);
            }

            return entity;
        }
    }

    internal static class BusinessApprovalPrerequisites
    {
        public static async Task ValidateAsync(
            IAppDbContext db,
            Business entity,
            IStringLocalizer<ValidationResource> localizer,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(entity.ContactEmail) || string.IsNullOrWhiteSpace(entity.LegalName))
            {
                throw new ValidationException(localizer["BusinessApprovalPrerequisitesMissing"]);
            }

            var hasActiveOwner = await db.Set<BusinessMember>()
                .AsNoTracking()
                .AnyAsync(x =>
                    x.BusinessId == entity.Id &&
                    !x.IsDeleted &&
                    x.IsActive &&
                    x.Role == BusinessMemberRole.Owner,
                    ct)
                .ConfigureAwait(false);
            if (!hasActiveOwner)
            {
                throw new ValidationException(localizer["BusinessApprovalPrerequisitesMissing"]);
            }

            var hasPrimaryLocation = await db.Set<BusinessLocation>()
                .AsNoTracking()
                .AnyAsync(x =>
                    x.BusinessId == entity.Id &&
                    !x.IsDeleted &&
                    x.IsPrimary,
                    ct)
                .ConfigureAwait(false);
            if (!hasPrimaryLocation)
            {
                throw new ValidationException(localizer["BusinessApprovalPrerequisitesMissing"]);
            }
        }
    }
}
