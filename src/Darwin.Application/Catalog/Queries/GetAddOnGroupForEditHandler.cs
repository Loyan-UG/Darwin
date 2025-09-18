using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Catalog.Queries
{
    /// <summary>
    /// Loads an add-on group as a full edit DTO (aggregate graph).
    /// </summary>
    public sealed class GetAddOnGroupForEditHandler
    {
        private readonly IAppDbContext _db;
        public GetAddOnGroupForEditHandler(IAppDbContext db) => _db = db;

        public async Task<AddOnGroupEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var g = await _db.Set<AddOnGroup>()
                .AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(x => new AddOnGroupEditDto
                {
                    Id = x.Id,
                    // RowVersion cannot be read via projection; will be set by controller from entity if needed.
                    RowVersion = Array.Empty<byte>(),
                    Name = x.Name,
                    Currency = x.Currency,
                    IsGlobal = x.IsGlobal,
                    SelectionMode = x.SelectionMode,
                    MinSelections = x.MinSelections,
                    MaxSelections = x.MaxSelections,
                    IsActive = x.IsActive,
                    Options = x.Options
                        .OrderBy(o => o.SortOrder)
                        .Select(o => new AddOnOptionDto
                        {
                            Label = o.Label,
                            SortOrder = o.SortOrder,
                            Values = o.Values
                                .OrderBy(v => v.SortOrder).Select(v => new AddOnOptionValueDto
                                {
                                    Label = v.Label,
                                    PriceDeltaMinor = v.PriceDeltaMinor,
                                    Hint = v.Hint,
                                    SortOrder = v.SortOrder,
                                    IsActive = v.IsActive
                                }).ToList()
                        }).ToList()
                }).FirstOrDefaultAsync(ct);

            return g;
        }
    }
}
