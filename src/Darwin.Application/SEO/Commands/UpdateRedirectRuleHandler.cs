using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.SEO.DTOs;
using Darwin.Application.SEO.Validators;
using Darwin.Domain.Entities.SEO;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.SEO.Commands
{
    /// <summary>
    /// Updates a redirect rule with optimistic concurrency and uniqueness.
    /// </summary>
    public sealed class UpdateRedirectRuleHandler
    {
        private readonly IAppDbContext _db;
        private readonly RedirectRuleEditValidator _vBasic = new();
        private readonly RedirectRuleEditUniqueValidator _vUnique;

        public UpdateRedirectRuleHandler(IAppDbContext db)
        {
            _db = db;
            _vUnique = new RedirectRuleEditUniqueValidator(db);
        }

        public async Task HandleAsync(RedirectRuleEditDto dto, CancellationToken ct = default)
        {
            var r1 = _vBasic.Validate(dto);
            if (!r1.IsValid) throw new ValidationException(r1.Errors);
            var r2 = await _vUnique.ValidateAsync(dto, ct);
            if (!r2.IsValid) throw new ValidationException(r2.Errors);

            var entity = await _db.Set<RedirectRule>().FirstOrDefaultAsync(r => r.Id == dto.Id && !r.IsDeleted, ct);
            if (entity is null) throw new InvalidOperationException("Redirect rule not found.");

            if (!entity.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");

            entity.FromPath = dto.FromPath.Trim();
            entity.To = dto.To.Trim();
            entity.IsPermanent = dto.IsPermanent;

            await _db.SaveChangesAsync(ct);
        }
    }
}
