using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Resets the user's password if the provided token is valid and not expired/used.
    /// Updates SecurityStamp to force sign-out of old sessions.
    /// </summary>
    public sealed class ResetPasswordHandler
    {
        private readonly IAppDbContext _db;
        private readonly IUserPasswordHasher _hasher;
        private readonly ISecurityStampService _stamps;
        private readonly IClock _clock;
        private readonly IValidator<ResetPasswordDto> _validator;

        public ResetPasswordHandler(IAppDbContext db, IUserPasswordHasher hasher, ISecurityStampService stamps,
                                    IClock clock, IValidator<ResetPasswordDto> validator)
        {
            _db = db; _hasher = hasher; _stamps = stamps; _clock = clock; _validator = validator;
        }

        public async Task<Result> HandleAsync(ResetPasswordDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Email == dto.Email && !u.IsDeleted, ct);
            if (user == null) return Result.Fail("Invalid token or email.");

            var token = await _db.Set<UserToken>()
                .Where(t => t.UserId == user.Id && t.Purpose == "PasswordReset" && t.Value == dto.Token)
                .FirstOrDefaultAsync(ct);

            if (token == null || token.UsedAtUtc != null || (token.ExpiresAtUtc.HasValue && token.ExpiresAtUtc.Value < _clock.UtcNow))
                return Result.Fail("Invalid or expired token.");

            user.PasswordHash = _hasher.Hash(dto.NewPassword);
            user.SecurityStamp = _stamps.NewStamp();
            token.MarkUsed(_clock.UtcNow);

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
