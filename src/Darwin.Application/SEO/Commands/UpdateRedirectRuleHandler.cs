using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.SEO.DTOs;
using Darwin.Application.SEO.Validators;
using Darwin.Domain.Entities.SEO;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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
        private readonly RedirectRuleEditValidator _vBasic;
        private readonly RedirectRuleEditUniqueValidator _vUnique;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateRedirectRuleHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
            _vBasic = new RedirectRuleEditValidator(localizer);
            _vUnique = new RedirectRuleEditUniqueValidator(db, localizer);
        }

        public async Task HandleAsync(RedirectRuleEditDto dto, CancellationToken ct = default)
        {
            dto.FromPath = dto.FromPath?.Trim() ?? string.Empty;
            dto.To = dto.To?.Trim() ?? string.Empty;

            var r1 = _vBasic.Validate(dto);
            if (!r1.IsValid) throw new ValidationException(r1.Errors);
            var r2 = await _vUnique.ValidateAsync(dto, ct);
            if (!r2.IsValid) throw new ValidationException(r2.Errors);

            var entity = await _db.Set<RedirectRule>().FirstOrDefaultAsync(r => r.Id == dto.Id && !r.IsDeleted, ct);
            if (entity is null) throw new InvalidOperationException(_localizer["RedirectRuleNotFound"]);

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);

            entity.FromPath = dto.FromPath.Trim();
            entity.To = dto.To.Trim();
            entity.IsPermanent = dto.IsPermanent;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }
        }
    }
}
