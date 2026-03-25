using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Entities.CRM;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CRM.Commands
{
    public sealed class CreateOpportunityHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<OpportunityCreateDto> _validator;

        public CreateOpportunityHandler(IAppDbContext db, IValidator<OpportunityCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Guid> HandleAsync(OpportunityCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var customerExists = await _db.Set<Customer>()
                .AsNoTracking()
                .AnyAsync(x => x.Id == dto.CustomerId, ct)
                .ConfigureAwait(false);

            if (!customerExists)
            {
                throw new InvalidOperationException("Customer not found.");
            }

            var opportunity = new Opportunity
            {
                CustomerId = dto.CustomerId,
                Title = dto.Title.Trim(),
                EstimatedValueMinor = dto.EstimatedValueMinor,
                Stage = dto.Stage,
                ExpectedCloseDateUtc = dto.ExpectedCloseDateUtc,
                AssignedToUserId = dto.AssignedToUserId,
                Items = dto.Items.Select(x => new OpportunityItem
                {
                    ProductVariantId = x.ProductVariantId,
                    Quantity = x.Quantity,
                    UnitPriceMinor = x.UnitPriceMinor
                }).ToList()
            };

            _db.Set<Opportunity>().Add(opportunity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return opportunity.Id;
        }
    }

    public sealed class UpdateOpportunityHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<OpportunityEditDto> _validator;

        public UpdateOpportunityHandler(IAppDbContext db, IValidator<OpportunityEditDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(OpportunityEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var opportunity = await _db.Set<Opportunity>()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (opportunity is null)
            {
                throw new InvalidOperationException("Opportunity not found.");
            }

            if (!opportunity.RowVersion.SequenceEqual(dto.RowVersion))
            {
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");
            }

            opportunity.CustomerId = dto.CustomerId;
            opportunity.Title = dto.Title.Trim();
            opportunity.EstimatedValueMinor = dto.EstimatedValueMinor;
            opportunity.Stage = dto.Stage;
            opportunity.ExpectedCloseDateUtc = dto.ExpectedCloseDateUtc;
            opportunity.AssignedToUserId = dto.AssignedToUserId;

            _db.Set<OpportunityItem>().RemoveRange(opportunity.Items);
            opportunity.Items = dto.Items.Select(x => new OpportunityItem
            {
                ProductVariantId = x.ProductVariantId,
                Quantity = x.Quantity,
                UnitPriceMinor = x.UnitPriceMinor
            }).ToList();

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
