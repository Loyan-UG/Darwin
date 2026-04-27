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
                    RowVersion = x.RowVersion,
                    Name = x.Name,
                    Currency = x.Currency,
                    IsGlobal = x.IsGlobal,
                    SelectionMode = x.SelectionMode,
                    MinSelections = x.MinSelections,
                    MaxSelections = x.MaxSelections,
                    IsActive = x.IsActive,
                    Translations = x.Translations
                        .OrderBy(t => t.Culture)
                        .Select(t => new AddOnGroupTranslationDto
                        {
                            Culture = t.Culture,
                            Name = t.Name
                        }).ToList(),
                    Options = x.Options
                        .OrderBy(o => o.SortOrder)
                        .Select(o => new AddOnOptionDto
                        {
                            Id = o.Id,
                            Label = o.Label,
                            SortOrder = o.SortOrder,
                            Translations = o.Translations
                                .OrderBy(t => t.Culture)
                                .Select(t => new AddOnOptionTranslationDto
                                {
                                    Culture = t.Culture,
                                    Label = t.Label
                                }).ToList(),
                            Values = o.Values
                                .OrderBy(v => v.SortOrder).Select(v => new AddOnOptionValueDto
                                {
                                    Id = v.Id,
                                    Label = v.Label,
                                    PriceDeltaMinor = v.PriceDeltaMinor,
                                    Hint = v.Hint,
                                    SortOrder = v.SortOrder,
                                    IsActive = v.IsActive,
                                    Translations = v.Translations
                                        .OrderBy(t => t.Culture)
                                        .Select(t => new AddOnOptionValueTranslationDto
                                        {
                                            Culture = t.Culture,
                                            Label = t.Label
                                        }).ToList()
                                }).ToList()
                        }).ToList()
                }).FirstOrDefaultAsync(ct);

            return g;
        }
    }
}
