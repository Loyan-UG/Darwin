using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.SEO;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.SEO.Commands
{
    /// <summary>
    /// Performs a soft delete to preserve audit trail.
    /// </summary>
    public sealed class DeleteRedirectRuleHandler
    {
        private readonly IAppDbContext _db;
        public DeleteRedirectRuleHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Set<RedirectRule>().FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);
            if (entity == null) return;

            entity.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
