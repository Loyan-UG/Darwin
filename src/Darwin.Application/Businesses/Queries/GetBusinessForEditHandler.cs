using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Loads a single business for editing, including RowVersion for concurrency.
    /// </summary>
    public sealed class GetBusinessForEditHandler
    {
        private readonly IAppDbContext _db;
        public GetBusinessForEditHandler(IAppDbContext db) => _db = db;

        public async Task<BusinessEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Set<Business>()
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new BusinessEditDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    Name = x.Name,
                    LegalName = x.LegalName,
                    TaxId = x.TaxId,
                    ShortDescription = x.ShortDescription,
                    WebsiteUrl = x.WebsiteUrl,
                    ContactEmail = x.ContactEmail,
                    ContactPhoneE164 = x.ContactPhoneE164,
                    Category = x.Category,
                    DefaultCurrency = x.DefaultCurrency,
                    DefaultCulture = x.DefaultCulture,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync(ct);
        }
    }
}
