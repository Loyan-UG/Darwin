using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.SEO.DTOs;
using Darwin.Domain.Entities.SEO;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.SEO.Validators
{
    /// <summary>
    /// Ensures FromPath is unique among non-deleted entries.
    /// </summary>
    public sealed class RedirectRuleCreateUniqueValidator : AbstractValidator<RedirectRuleCreateDto>
    {
        private readonly IAppDbContext _db;

        public RedirectRuleCreateUniqueValidator(IAppDbContext db)
        {
            _db = db;
            RuleFor(x => x.FromPath).MustAsync(BeUnique).WithMessage("FromPath must be unique.");
        }

        private async Task<bool> BeUnique(string from, CancellationToken ct)
        {
            return !await _db.Set<RedirectRule>().AsNoTracking()
                .AnyAsync(r => !r.IsDeleted && r.FromPath == from, ct);
        }
    }

    public sealed class RedirectRuleEditUniqueValidator : AbstractValidator<RedirectRuleEditDto>
    {
        private readonly IAppDbContext _db;

        public RedirectRuleEditUniqueValidator(IAppDbContext db)
        {
            _db = db;
            RuleFor(x => x).MustAsync(BeUnique).WithMessage("FromPath must be unique.");
        }

        private async Task<bool> BeUnique(RedirectRuleEditDto dto, CancellationToken ct)
        {
            return !await _db.Set<RedirectRule>().AsNoTracking()
                .AnyAsync(r => !r.IsDeleted && r.FromPath == dto.FromPath && r.Id != dto.Id, ct);
        }
    }
}
