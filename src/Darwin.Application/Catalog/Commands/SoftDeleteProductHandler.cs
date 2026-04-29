using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Catalog;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Performs a soft delete of a <see cref="Product"/> by setting <c>IsDeleted = true</c>.
    /// </summary>
    public sealed class SoftDeleteProductHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public SoftDeleteProductHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(Guid id, byte[]? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
                return Result.Fail(_localizer["InvalidDeleteRequest"]);

            if (rowVersion is null || rowVersion.Length == 0)
                return Result.Fail(_localizer["RowVersionRequired"]);

            var product = await _db.Set<Product>()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

            if (product is null)
                return Result.Fail(_localizer["ProductNotFound"]);

            var currentVersion = product.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(rowVersion))
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);

            product.IsDeleted = true;
            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            return Result.Ok();
        }
    }
}
