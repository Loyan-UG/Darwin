using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Commands
{
    /// <summary>
    /// Updates an existing <see cref="BusinessMember"/> with optimistic concurrency.
    /// </summary>
    public sealed class UpdateBusinessMemberHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<BusinessMemberEditDto> _validator;

        public UpdateBusinessMemberHandler(IAppDbContext db, IValidator<BusinessMemberEditDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(BusinessMemberEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await _db.Set<BusinessMember>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (entity is null)
                throw new InvalidOperationException("Business member not found.");

            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            var requestVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(requestVersion))
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");

            entity.Role = dto.Role;
            entity.IsActive = dto.IsActive;

            await _db.SaveChangesAsync(ct);
        }
    }
}
