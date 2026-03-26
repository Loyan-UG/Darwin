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

        public ApproveBusinessHandler(IAppDbContext db, IClock clock, IValidator<BusinessLifecycleActionDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(BusinessLifecycleActionDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await LoadBusinessAsync(dto, ct);

            entity.OperationalStatus = BusinessOperationalStatus.Approved;
            entity.ApprovedAtUtc ??= _clock.UtcNow;
            entity.SuspendedAtUtc = null;
            entity.SuspensionReason = null;
            entity.IsActive = true;

            await _db.SaveChangesAsync(ct);
        }

        private async Task<Business> LoadBusinessAsync(BusinessLifecycleActionDto dto, CancellationToken ct)
        {
            var entity = await _db.Set<Business>().FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (entity is null)
            {
                throw new InvalidOperationException("Business not found.");
            }

            if (!(entity.RowVersion ?? Array.Empty<byte>()).SequenceEqual(dto.RowVersion ?? Array.Empty<byte>()))
            {
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");
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

        public SuspendBusinessHandler(IAppDbContext db, IClock clock, IValidator<BusinessLifecycleActionDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(BusinessLifecycleActionDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await LoadBusinessAsync(dto, ct);

            entity.OperationalStatus = BusinessOperationalStatus.Suspended;
            entity.SuspendedAtUtc = _clock.UtcNow;
            entity.SuspensionReason = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
            entity.IsActive = false;

            await _db.SaveChangesAsync(ct);
        }

        private async Task<Business> LoadBusinessAsync(BusinessLifecycleActionDto dto, CancellationToken ct)
        {
            var entity = await _db.Set<Business>().FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (entity is null)
            {
                throw new InvalidOperationException("Business not found.");
            }

            if (!(entity.RowVersion ?? Array.Empty<byte>()).SequenceEqual(dto.RowVersion ?? Array.Empty<byte>()))
            {
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");
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

        public ReactivateBusinessHandler(IAppDbContext db, IClock clock, IValidator<BusinessLifecycleActionDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(BusinessLifecycleActionDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await LoadBusinessAsync(dto, ct);

            entity.OperationalStatus = BusinessOperationalStatus.Approved;
            entity.ApprovedAtUtc ??= _clock.UtcNow;
            entity.SuspendedAtUtc = null;
            entity.SuspensionReason = null;
            entity.IsActive = true;

            await _db.SaveChangesAsync(ct);
        }

        private async Task<Business> LoadBusinessAsync(BusinessLifecycleActionDto dto, CancellationToken ct)
        {
            var entity = await _db.Set<Business>().FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (entity is null)
            {
                throw new InvalidOperationException("Business not found.");
            }

            if (!(entity.RowVersion ?? Array.Empty<byte>()).SequenceEqual(dto.RowVersion ?? Array.Empty<byte>()))
            {
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");
            }

            return entity;
        }
    }
}
