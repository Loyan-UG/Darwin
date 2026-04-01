using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Commands
{
    /// <summary>
    /// Updates an existing <see cref="BusinessMember"/> with optimistic concurrency.
    /// </summary>
    public sealed class UpdateBusinessMemberHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<BusinessMemberEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateBusinessMemberHandler(
            IAppDbContext db,
            IValidator<BusinessMemberEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(BusinessMemberEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await _db.Set<BusinessMember>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (entity is null)
                throw new InvalidOperationException(_localizer["BusinessMemberNotFound"]);

            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            var requestVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(requestVersion))
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);

            var isOwnerBeingDemotedOrDisabled =
                entity.Role == BusinessMemberRole.Owner &&
                (dto.Role != BusinessMemberRole.Owner || !dto.IsActive);

            if (isOwnerBeingDemotedOrDisabled)
            {
                var hasAnotherActiveOwner = await _db.Set<BusinessMember>()
                    .AnyAsync(x =>
                        x.BusinessId == entity.BusinessId &&
                        x.Id != entity.Id &&
                        x.Role == BusinessMemberRole.Owner &&
                        x.IsActive, ct);

                if (!hasAnotherActiveOwner)
                {
                    if (!dto.AllowLastOwnerOverride)
                        throw new InvalidOperationException(_localizer["AtLeastOneActiveOwnerMustRemainAssignedToBusiness"]);

                    _db.Set<BusinessOwnerOverrideAudit>().Add(new BusinessOwnerOverrideAudit
                    {
                        BusinessId = entity.BusinessId,
                        BusinessMemberId = entity.Id,
                        AffectedUserId = entity.UserId,
                        ActionKind = BusinessOwnerOverrideActionKind.DemoteOrDeactivate,
                        Reason = dto.OverrideReason!.Trim(),
                        ActorDisplayName = string.IsNullOrWhiteSpace(dto.OverrideActorDisplayName) ? null : dto.OverrideActorDisplayName.Trim()
                    });
                }
            }

            entity.Role = dto.Role;
            entity.IsActive = dto.IsActive;

            await _db.SaveChangesAsync(ct);
        }
    }
}
