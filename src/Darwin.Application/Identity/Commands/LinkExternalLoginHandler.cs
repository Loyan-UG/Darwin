using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Links an external provider identity to an existing user (idempotent).
    /// Provider+ProviderKey must be unique per user.
    /// </summary>
    public sealed class LinkExternalLoginHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<LinkExternalLoginDto> _validator;

        public LinkExternalLoginHandler(IAppDbContext db, IValidator<LinkExternalLoginDto> validator)
        {
            _db = db; _validator = validator;
        }

        public async Task<Result> HandleAsync(LinkExternalLoginDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted, ct);
            if (user == null) return Result.Fail("User not found.");

            var exists = await _db.Set<UserLogin>()
                .AnyAsync(l => l.UserId == dto.UserId && l.Provider == dto.Provider && l.ProviderKey == dto.ProviderKey && !l.IsDeleted, ct);

            if (!exists)
            {
                _db.Set<UserLogin>().Add(new UserLogin(dto.UserId, dto.Provider, dto.ProviderKey, dto.DisplayName));
                await _db.SaveChangesAsync(ct);
            }

            return Result.Ok();
        }
    }
}
