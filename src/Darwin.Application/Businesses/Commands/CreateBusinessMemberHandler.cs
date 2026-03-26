using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Commands
{
    /// <summary>
    /// Creates a new <see cref="BusinessMember"/> link between Business and User.
    /// Hard delete is allowed for offboarding.
    /// </summary>
    public sealed class CreateBusinessMemberHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<BusinessMemberCreateDto> _validator;

        public CreateBusinessMemberHandler(IAppDbContext db, IValidator<BusinessMemberCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Guid> HandleAsync(BusinessMemberCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var businessExists = await _db.Set<Business>()
                .AnyAsync(x => x.Id == dto.BusinessId, ct);
            if (!businessExists)
                throw new InvalidOperationException("Business not found.");

            var userExists = await _db.Set<User>()
                .AnyAsync(x => x.Id == dto.UserId && !x.IsDeleted, ct);
            if (!userExists)
                throw new InvalidOperationException("User not found.");

            var duplicateExists = await _db.Set<BusinessMember>()
                .AnyAsync(x => x.BusinessId == dto.BusinessId && x.UserId == dto.UserId, ct);
            if (duplicateExists)
                throw new InvalidOperationException("This user is already assigned to the selected business.");

            var entity = new BusinessMember
            {
                BusinessId = dto.BusinessId,
                UserId = dto.UserId,
                Role = dto.Role,
                IsActive = dto.IsActive
            };

            _db.Set<BusinessMember>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }
    }
}
