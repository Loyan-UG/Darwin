using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Auth.Commands
{
    /// <summary>
    /// Validates credentials and returns a SignInResultDto.
    /// NOTE: Creating the authentication cookie / claims principal is NOT done here.
    /// Web layer should take UserId + SecurityStamp and issue the cookie.
    /// </summary>
    public sealed class SignInHandler
    {
        private readonly IAppDbContext _db;
        private readonly IUserPasswordHasher _hasher;
        private readonly IValidator<SignInDto> _validator;

        public SignInHandler(IAppDbContext db, IUserPasswordHasher hasher, IValidator<SignInDto> validator)
        {
            _db = db; _hasher = hasher; _validator = validator;
        }

        public async Task<SignInResultDto> HandleAsync(SignInDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Email == dto.Email && !u.IsDeleted, ct);
            if (user == null || !user.IsActive)
                return new SignInResultDto { Succeeded = false, FailureReason = "Invalid credentials." };

            if (!_hasher.Verify(user.PasswordHash, dto.Password))
                return new SignInResultDto { Succeeded = false, FailureReason = "Invalid credentials." };

            // TODO: If 2FA enabled on user, enforce it and return RequiresTwoFactor=true.
            var twoFactorEnabled = user.TwoFactorEnabled; // Assuming Domain has this flag.
            if (twoFactorEnabled)
            {
                return new SignInResultDto
                {
                    Succeeded = false,
                    RequiresTwoFactor = true,
                    TwoFactorDelivery = "TOTP", // For now we standardize on TOTP app
                    UserId = user.Id
                };
            }

            return new SignInResultDto
            {
                Succeeded = true,
                UserId = user.Id,
                SecurityStamp = user.SecurityStamp
            };
        }
    }
}
