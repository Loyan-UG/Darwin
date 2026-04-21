using System;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Orders.Validators
{
    public sealed class OrderCreateValidator : AbstractValidator<OrderCreateDto>
    {
        public OrderCreateValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Currency).NotEmpty().Length(3);
            RuleFor(x => x.BillingAddressJson).NotEmpty();
            RuleFor(x => x.ShippingAddressJson).NotEmpty();
            RuleFor(x => x.Lines).NotEmpty();
            RuleForEach(x => x.Lines).SetValidator(new OrderLineCreateValidator(localizer));

            RuleFor(x => x.ShippingTotalMinor).GreaterThanOrEqualTo(0);
            RuleFor(x => x.DiscountTotalMinor).GreaterThanOrEqualTo(0);
        }
    }

    public sealed class OrderLineCreateValidator : AbstractValidator<OrderLineCreateDto>
    {
        public OrderLineCreateValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.VariantId).NotEmpty();
            RuleFor(x => x.WarehouseId)
                .Must(x => !x.HasValue || x.Value != Guid.Empty)
                .WithMessage(localizer["WarehouseIdValidWhenProvided"]);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(512);
            RuleFor(x => x.Sku).NotEmpty().MaximumLength(128);
            RuleFor(x => x.Quantity).GreaterThan(0);
            RuleFor(x => x.UnitPriceNetMinor).GreaterThanOrEqualTo(0);
            RuleFor(x => x.VatRate).InclusiveBetween(0m, 1m);
        }
    }

    public sealed class PaymentCreateValidator : AbstractValidator<PaymentCreateDto>
    {
        public PaymentCreateValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.Provider).NotEmpty().MaximumLength(64);
            RuleFor(x => x.ProviderReference).NotEmpty().MaximumLength(256);
            RuleFor(x => x.AmountMinor).GreaterThan(0);
            RuleFor(x => x.Currency).NotEmpty().Length(3);
            RuleFor(x => x.Status).IsInEnum();

            When(x => x.Status == PaymentStatus.Failed, () =>
            {
                RuleFor(x => x.FailureReason)
                    .NotEmpty()
                    .MaximumLength(512);
            });
        }
    }

    public sealed class ShipmentCreateValidator : AbstractValidator<ShipmentCreateDto>
    {
        public ShipmentCreateValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.Carrier).NotEmpty().MaximumLength(64);
            RuleFor(x => x.Service).NotEmpty().MaximumLength(64);
            RuleForEach(x => x.Lines).SetValidator(new ShipmentLineCreateValidator());
        }
    }

    public sealed class ShipmentLineCreateValidator : AbstractValidator<ShipmentLineCreateDto>
    {
        public ShipmentLineCreateValidator()
        {
            RuleFor(x => x.OrderLineId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0);
        }
    }

    public sealed class ApplyShipmentCarrierEventValidator : AbstractValidator<ApplyShipmentCarrierEventDto>
    {
        public ApplyShipmentCarrierEventValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Carrier).NotEmpty().MaximumLength(64);
            RuleFor(x => x.ProviderShipmentReference).NotEmpty().MaximumLength(128);
            RuleFor(x => x.TrackingNumber).MaximumLength(128).When(x => !string.IsNullOrWhiteSpace(x.TrackingNumber));
            RuleFor(x => x.LabelUrl).MaximumLength(2048).When(x => !string.IsNullOrWhiteSpace(x.LabelUrl));
            RuleFor(x => x.Service).MaximumLength(64).When(x => !string.IsNullOrWhiteSpace(x.Service));
            RuleFor(x => x.CarrierEventKey).NotEmpty().MaximumLength(128);
            RuleFor(x => x.OccurredAtUtc)
                .Must(x => x != default)
                .WithMessage(localizer["CarrierEventOccurredAtUtcRequired"]);
            RuleFor(x => x.ProviderStatus).MaximumLength(64).When(x => !string.IsNullOrWhiteSpace(x.ProviderStatus));
            RuleFor(x => x.ExceptionCode).MaximumLength(128).When(x => !string.IsNullOrWhiteSpace(x.ExceptionCode));
            RuleFor(x => x.ExceptionMessage).MaximumLength(512).When(x => !string.IsNullOrWhiteSpace(x.ExceptionMessage));
        }
    }

    public sealed class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusDto>
    {
        public UpdateOrderStatusValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
            RuleFor(x => x.NewStatus).IsInEnum();
            RuleFor(x => x.WarehouseId)
                .Must(x => !x.HasValue || x.Value != Guid.Empty)
                .WithMessage(localizer["WarehouseIdValidWhenProvided"]);
        }
    }

    public sealed class RefundCreateValidator : AbstractValidator<RefundCreateDto>
    {
        public RefundCreateValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.PaymentId).NotEmpty();
            RuleFor(x => x.AmountMinor).GreaterThan(0);
            RuleFor(x => x.Currency).NotEmpty().Length(3);
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(256);
        }
    }

    public sealed class OrderInvoiceCreateValidator : AbstractValidator<OrderInvoiceCreateDto>
    {
        public OrderInvoiceCreateValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.BusinessId)
                .Must(x => !x.HasValue || x.Value != Guid.Empty)
                .WithMessage(localizer["BusinessIdValidWhenProvided"]);
            RuleFor(x => x.CustomerId)
                .Must(x => !x.HasValue || x.Value != Guid.Empty)
                .WithMessage(localizer["CustomerIdValidWhenProvided"]);
            RuleFor(x => x.PaymentId)
                .Must(x => !x.HasValue || x.Value != Guid.Empty)
                .WithMessage(localizer["PaymentIdValidWhenProvided"]);
        }
    }
}
