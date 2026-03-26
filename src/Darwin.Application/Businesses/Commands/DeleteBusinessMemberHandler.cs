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

namespace Darwin.Application.Businesses.Commands
{
    /// <summary>
    /// Hard-deletes a <see cref="BusinessMember"/> link while protecting the last active owner assignment.
    /// </summary>
    public sealed class DeleteBusinessMemberHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<BusinessMemberDeleteDto> _validator;

        public DeleteBusinessMemberHandler(IAppDbContext db, IValidator<BusinessMemberDeleteDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(BusinessMemberDeleteDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await _db.Set<BusinessMember>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (entity is null) return;

            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            var requestVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(requestVersion))
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");

            if (entity.Role == BusinessMemberRole.Owner && entity.IsActive)
            {
                var hasAnotherActiveOwner = await _db.Set<BusinessMember>()
                    .AnyAsync(x =>
                        x.BusinessId == entity.BusinessId &&
                        x.Id != entity.Id &&
                        x.Role == BusinessMemberRole.Owner &&
                        x.IsActive, ct);

                if (!hasAnotherActiveOwner)
                    throw new InvalidOperationException("At least one active owner must remain assigned to the business.");
            }

            _db.Set<BusinessMember>().Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
