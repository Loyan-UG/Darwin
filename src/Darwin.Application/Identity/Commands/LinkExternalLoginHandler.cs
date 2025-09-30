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
    /// Links an external provider identity (e.g., Google, Microsoft) to an existing user.
    /// The operation is idempotent: if the link already exists and is not soft-deleted, no changes are made.
    /// </summary>
    public sealed class LinkExternalLoginHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<LinkExternalLoginDto> _validator;

        /// <summary>
        /// Creates a new instance of the handler.
        /// </summary>
        /// <param name="db">Application DbContext abstraction used to access identity tables.</param>
        /// <param name="validator">FluentValidation validator for <see cref="LinkExternalLoginDto"/>.</param>
        public LinkExternalLoginHandler(IAppDbContext db, IValidator<LinkExternalLoginDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        /// <summary>
        /// Associates an external provider key with the specified user.
        /// </summary>
        /// <param name="dto">The DTO containing <c>UserId</c>, <c>Provider</c>, and <c>ProviderKey</c>.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns><see cref="Result.Ok"/> on success or if the link already exists.</returns>
        public async Task<Result> HandleAsync(LinkExternalLoginDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted, ct);
            if (user == null)
                return Result.Fail("User not found.");

            var exists = await _db.Set<UserLogin>()
                .AnyAsync(l => l.UserId == dto.UserId &&
                               l.Provider == dto.Provider &&
                               l.ProviderKey == dto.ProviderKey &&
                               !l.IsDeleted, ct);

            if (!exists)
            {
                _db.Set<UserLogin>().Add(new UserLogin(dto.UserId, dto.Provider, dto.ProviderKey, dto.DisplayName));
                await _db.SaveChangesAsync(ct);
            }

            return Result.Ok();
        }
    }
}
