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
    /// Disables 2FA by removing (or marking deleted) all TOTP secrets of the user and flipping the flag.
    /// </summary>
    public sealed class DisableTotpHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<DisableTotpDto> _validator;

        public DisableTotpHandler(IAppDbContext db, IValidator<DisableTotpDto> validator)
        {
            _db = db; _validator = validator;
        }

        public async Task<Result> HandleAsync(DisableTotpDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted, ct);
            if (user == null) return Result.Fail("User not found.");

            var secrets = await _db.Set<UserTwoFactorSecret>()
                .Where(s => s.UserId == dto.UserId && !s.IsDeleted)
                .ToListAsync(ct);

            foreach (var s in secrets) s.IsDeleted = true;
            user.TwoFactorEnabled = false;

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
