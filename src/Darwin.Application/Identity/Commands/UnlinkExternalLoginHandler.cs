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
    /// Removes (soft-deletes) an external provider link from the specified user.
    /// </summary>
    public sealed class UnlinkExternalLoginHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<UnlinkExternalLoginDto> _validator;

        /// <summary>
        /// Creates a new instance of the handler.
        /// </summary>
        /// <param name="db">Application DbContext abstraction used to access identity tables.</param>
        /// <param name="validator">FluentValidation validator for <see cref="UnlinkExternalLoginDto"/>.</param>
        public UnlinkExternalLoginHandler(IAppDbContext db, IValidator<UnlinkExternalLoginDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        /// <summary>
        /// Soft-deletes a single external login link identified by provider and provider key.
        /// </summary>
        /// <param name="dto">The DTO containing <c>UserId</c>, <c>Provider</c>, and <c>ProviderKey</c>.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns><see cref="Result.Ok"/> on success; <see cref="Result.Fail(string)"/> when link not found.</returns>
        public async Task<Result> HandleAsync(UnlinkExternalLoginDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var link = await _db.Set<UserLogin>()
                .FirstOrDefaultAsync(l => l.UserId == dto.UserId &&
                                          l.Provider == dto.Provider &&
                                          l.ProviderKey == dto.ProviderKey &&
                                          !l.IsDeleted, ct);

            if (link == null)
                return Result.Fail("External link not found.");

            link.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
