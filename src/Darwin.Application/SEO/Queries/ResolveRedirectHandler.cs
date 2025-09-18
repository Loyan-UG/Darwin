using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.SEO.DTOs;
using Darwin.Domain.Entities.SEO;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.SEO.Queries
{
    /// <summary>
    /// Resolves a request path to a redirect target if a rule exists; otherwise returns null.
    /// Intended to be called by a Web middleware.
    /// </summary>
    public sealed class ResolveRedirectHandler
    {
        private readonly IAppDbContext _db;
        public ResolveRedirectHandler(IAppDbContext db) => _db = db;

        public async Task<RedirectResolveResult?> HandleAsync(string requestPath, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(requestPath)) return null;

            var rule = await _db.Set<RedirectRule>().AsNoTracking()
                .FirstOrDefaultAsync(r => !r.IsDeleted && r.FromPath == requestPath, ct);

            if (rule == null) return null;

            return new RedirectResolveResult
            {
                To = rule.To,
                IsPermanent = rule.IsPermanent
            };
        }
    }
}
