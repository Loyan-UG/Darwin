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
    /// <summary>Removes an external provider link from a user.</summary>
    public sealed class UnlinkExternalLoginHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<UnlinkExternalLoginDto> _validator;

        public UnlinkExternalLoginHandler(IAppDbContext db, IValidator<UnlinkExternalLoginDto> validator)
        {
            _db = db; _validator = validator;
        }

        public async Task<Result> HandleAsync(UnlinkExternalLoginDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var link = await _db.Set<UserLogin>()
                .FirstOrDefaultAsync(l => l.UserId == dto.UserId && l.Provider == dto.Provider && l.ProviderKey == dto.ProviderKey && !l.IsDeleted, ct);

            if (link == null) return Result.Fail("External link not found.");

            link.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
