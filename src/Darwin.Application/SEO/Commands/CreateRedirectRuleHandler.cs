using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.SEO.DTOs;
using Darwin.Application.SEO.Validators;
using Darwin.Domain.Entities.SEO;
using FluentValidation;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.SEO.Commands
{
    /// <summary>
    /// Creates a redirect rule after validating format and uniqueness.
    /// </summary>
    public sealed class CreateRedirectRuleHandler
    {
        private readonly IAppDbContext _db;
        private readonly RedirectRuleCreateValidator _vBasic = new();
        private readonly RedirectRuleCreateUniqueValidator _vUnique;

        public CreateRedirectRuleHandler(IAppDbContext db)
        {
            _db = db;
            _vUnique = new RedirectRuleCreateUniqueValidator(db);
        }

        public async Task HandleAsync(RedirectRuleCreateDto dto, CancellationToken ct = default)
        {
            var r1 = _vBasic.Validate(dto);
            if (!r1.IsValid) throw new ValidationException(r1.Errors);
            var r2 = await _vUnique.ValidateAsync(dto, ct);
            if (!r2.IsValid) throw new ValidationException(r2.Errors);

            var entity = new RedirectRule
            {
                FromPath = dto.FromPath.Trim(),
                To = dto.To.Trim(),
                IsPermanent = dto.IsPermanent
            };

            _db.Set<RedirectRule>().Add(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
