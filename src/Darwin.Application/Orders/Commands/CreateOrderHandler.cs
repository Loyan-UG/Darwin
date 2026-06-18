using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Services;
using Darwin.Domain.Enums;
using FluentValidation;

namespace Darwin.Application.Orders.Commands
{
    /// <summary>
    /// Creates an order from provided lines and totals. Totals are computed server-side to ensure consistency.
    /// </summary>
    public sealed class CreateOrderHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<OrderCreateDto> _validator;
        private readonly OrderCreationService _orderCreation;

        public CreateOrderHandler(
            IAppDbContext db,
            IClock clock,
            IValidator<OrderCreateDto> validator,
            NumberSequenceService? numberSequenceService = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _orderCreation = new OrderCreationService(db, clock ?? throw new ArgumentNullException(nameof(clock)), numberSequenceService);
        }

        public async Task<Guid> HandleAsync(OrderCreateDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid)
            {
                throw new ValidationException(v.Errors);
            }

            var order = await _orderCreation.CreateAsync(new OrderCreationRequest
            {
                UserId = dto.UserId,
                BusinessId = dto.BusinessId,
                CustomerId = dto.CustomerId,
                Currency = dto.Currency,
                PricesIncludeTax = dto.PricesIncludeTax,
                SalesChannel = SalesChannel.Admin,
                BillingAddressJson = dto.BillingAddressJson,
                ShippingAddressJson = dto.ShippingAddressJson,
                ShippingTotalMinor = dto.ShippingTotalMinor,
                DiscountTotalMinor = dto.DiscountTotalMinor,
                Lines = dto.Lines.Select(line =>
                {
                    var unitGross = line.UnitPriceNetMinor + (long)Math.Round(line.UnitPriceNetMinor * (double)line.VatRate, MidpointRounding.AwayFromZero);
                    var lineGross = unitGross * line.Quantity;
                    var lineTax = lineGross - (line.UnitPriceNetMinor * line.Quantity);

                    return new OrderCreationLineRequest
                    {
                        VariantId = line.VariantId,
                        WarehouseId = line.WarehouseId,
                        Name = line.Name,
                        Sku = line.Sku,
                        Quantity = line.Quantity,
                        UnitPriceNetMinor = line.UnitPriceNetMinor,
                        VatRate = line.VatRate,
                        UnitPriceGrossMinor = unitGross,
                        LineTaxMinor = lineTax,
                        LineGrossMinor = lineGross
                    };
                }).ToList()
            }, ct).ConfigureAwait(false);

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return order.Id;
        }
    }
}
