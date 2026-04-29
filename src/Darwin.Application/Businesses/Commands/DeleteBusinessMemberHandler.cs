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
    /// Hard-deletes a <see cref="BusinessMember"/> link while protecting the last active owner assignment.
    /// </summary>
    public sealed class DeleteBusinessMemberHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<BusinessMemberDeleteDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public DeleteBusinessMemberHandler(
            IAppDbContext db,
            IValidator<BusinessMemberDeleteDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(BusinessMemberDeleteDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await _db.Set<BusinessMember>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (entity is null) return;

            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            var requestVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (requestVersion.Length == 0 || !currentVersion.SequenceEqual(requestVersion))
                throw new ValidationException(_localizer["ConcurrencyConflictDetected"]);

            if (entity.Role == BusinessMemberRole.Owner && entity.IsActive)
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
                        ActionKind = BusinessOwnerOverrideActionKind.ForceRemove,
                        Reason = dto.OverrideReason!.Trim(),
                        ActorDisplayName = string.IsNullOrWhiteSpace(dto.OverrideActorDisplayName) ? null : dto.OverrideActorDisplayName.Trim()
                    });
                }
            }

            _db.Set<BusinessMember>().Remove(entity);
            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ValidationException(_localizer["ConcurrencyConflictDetected"]);
            }
        }
    }
}
