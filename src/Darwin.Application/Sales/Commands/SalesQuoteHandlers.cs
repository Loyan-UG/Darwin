using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Orders.Services;
using Darwin.Application.Sales.DTOs;
using Darwin.Application.Sales.Services;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Sales.Commands;

public sealed class CreateSalesQuoteHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<SalesQuoteCreateDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public CreateSalesQuoteHandler(IAppDbContext db, IValidator<SalesQuoteCreateDto> validator, IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task<Guid> HandleAsync(SalesQuoteCreateDto dto, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        await SalesQuoteGuard.ValidateLinksAsync(_db, dto.BusinessId, dto.CustomerId, dto.OpportunityId, _localizer, ct).ConfigureAwait(false);

        var quote = SalesQuoteMapper.CreateEntity(dto);
        _db.Set<SalesQuote>().Add(quote);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return quote.Id;
    }
}

public sealed class UpdateSalesQuoteHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<SalesQuoteEditDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public UpdateSalesQuoteHandler(IAppDbContext db, IValidator<SalesQuoteEditDto> validator, IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task HandleAsync(SalesQuoteEditDto dto, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        var quote = await _db.Set<SalesQuote>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (quote is null)
        {
            throw new InvalidOperationException(_localizer["SalesQuoteNotFound"]);
        }

        SalesQuoteGuard.EnsureRowVersion(quote.RowVersion, dto.RowVersion, _localizer);
        if (quote.Status != SalesQuoteStatus.Draft)
        {
            throw new ValidationException(_localizer["SalesQuoteDraftOnlyEdit"]);
        }

        await SalesQuoteGuard.ValidateLinksAsync(_db, dto.BusinessId, dto.CustomerId, dto.OpportunityId, _localizer, ct).ConfigureAwait(false);
        SalesQuoteMapper.UpdateEntity(quote, dto);

        try
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
        }
    }
}

public sealed class SendSalesQuoteHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<SalesQuoteLifecycleDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly NumberSequenceService _numberSequence;
    private readonly SalesQuoteLifecycleEventService _events;

    public SendSalesQuoteHandler(
        IAppDbContext db,
        IValidator<SalesQuoteLifecycleDto> validator,
        IStringLocalizer<ValidationResource> localizer,
        IClock clock,
        NumberSequenceService numberSequence,
        SalesQuoteLifecycleEventService events)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _numberSequence = numberSequence ?? throw new ArgumentNullException(nameof(numberSequence));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public async Task HandleAsync(SalesQuoteLifecycleDto dto, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        var quote = await LoadQuoteAsync(dto.Id, ct).ConfigureAwait(false);
        SalesQuoteGuard.EnsureRowVersion(quote.RowVersion, dto.RowVersion, _localizer);
        if (quote.Status != SalesQuoteStatus.Draft)
        {
            throw new ValidationException(_localizer["SalesQuoteSendRequiresDraft"]);
        }

        if (!quote.Lines.Any(x => !x.IsDeleted))
        {
            throw new ValidationException(_localizer["SalesQuoteRequiresLines"]);
        }

        if (string.IsNullOrWhiteSpace(quote.QuoteNumber))
        {
            var number = await ReserveQuoteNumberAsync(quote.BusinessId, ct).ConfigureAwait(false);
            quote.QuoteNumber = number;
        }

        quote.Status = SalesQuoteStatus.Sent;
        quote.SentAtUtc = _clock.UtcNow;
        quote.SentByUserId = dto.ActorUserId;
        await SaveAndRecordAsync(quote, "sales.quote.sent", SalesQuoteStatus.Draft, SalesQuoteStatus.Sent, dto.ActorUserId, dto.Reason, ct).ConfigureAwait(false);
    }

    private async Task<SalesQuote> LoadQuoteAsync(Guid id, CancellationToken ct)
    {
        var quote = await _db.Set<SalesQuote>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        return quote ?? throw new InvalidOperationException(_localizer["SalesQuoteNotFound"]);
    }

    private async Task<string> ReserveQuoteNumberAsync(Guid? businessId, CancellationToken ct)
    {
        var result = await _numberSequence
            .ReserveNextAsync(new NumberSequenceRequest(businessId, NumberSequenceDocumentType.SalesQuote, NumberSequenceService.GlobalScopeKey), ct)
            .ConfigureAwait(false);
        if (!result.Succeeded && businessId.HasValue)
        {
            result = await _numberSequence
                .ReserveNextAsync(new NumberSequenceRequest(null, NumberSequenceDocumentType.SalesQuote, NumberSequenceService.GlobalScopeKey), ct)
                .ConfigureAwait(false);
        }

        if (!result.Succeeded)
        {
            throw new ValidationException(_localizer["SalesQuoteNumberSequenceRequired"]);
        }

        return result.Value!;
    }

    private async Task SaveAndRecordAsync(SalesQuote quote, string eventType, SalesQuoteStatus from, SalesQuoteStatus to, Guid? actorUserId, string? reason, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await _events.RecordStatusChangedAsync(quote, eventType, from, to, actorUserId, reason, ct).ConfigureAwait(false);
    }
}

public sealed class UpdateSalesQuoteLifecycleHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<SalesQuoteLifecycleDto> _validator;
    private readonly IValidator<SalesQuoteConvertDto> _convertValidator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly SalesQuoteLifecycleEventService _events;

    public UpdateSalesQuoteLifecycleHandler(
        IAppDbContext db,
        IValidator<SalesQuoteLifecycleDto> validator,
        IValidator<SalesQuoteConvertDto> convertValidator,
        IStringLocalizer<ValidationResource> localizer,
        IClock clock,
        SalesQuoteLifecycleEventService events)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _convertValidator = convertValidator ?? throw new ArgumentNullException(nameof(convertValidator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Task AcceptAsync(SalesQuoteLifecycleDto dto, CancellationToken ct = default)
        => TransitionAsync(dto, SalesQuoteStatus.Sent, SalesQuoteStatus.Accepted, "sales.quote.accepted", q =>
        {
            q.AcceptedAtUtc = _clock.UtcNow;
            q.AcceptedByUserId = dto.ActorUserId;
        }, ct);

    public Task RejectAsync(SalesQuoteLifecycleDto dto, CancellationToken ct = default)
        => TransitionAsync(dto, SalesQuoteStatus.Sent, SalesQuoteStatus.Rejected, "sales.quote.rejected", q =>
        {
            q.RejectedAtUtc = _clock.UtcNow;
            q.RejectedByUserId = dto.ActorUserId;
        }, ct);

    public Task ExpireAsync(SalesQuoteLifecycleDto dto, CancellationToken ct = default)
        => TransitionAsync(dto, SalesQuoteStatus.Sent, SalesQuoteStatus.Expired, "sales.quote.expired", q =>
        {
            q.ExpiredAtUtc = _clock.UtcNow;
        }, ct);

    public async Task ConvertAsync(SalesQuoteConvertDto dto, CancellationToken ct = default)
    {
        await _convertValidator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        var quote = await LoadQuoteAsync(dto.Id, ct).ConfigureAwait(false);
        SalesQuoteGuard.EnsureRowVersion(quote.RowVersion, dto.RowVersion, _localizer);
        if (quote.Status != SalesQuoteStatus.Accepted)
        {
            throw new ValidationException(_localizer["SalesQuoteConvertRequiresAccepted"]);
        }

        var order = await _db.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.ConvertedOrderId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (order is null)
        {
            throw new ValidationException(_localizer["OrderNotFound"]);
        }

        if (quote.BusinessId.HasValue && order.BusinessId.HasValue && quote.BusinessId.Value != order.BusinessId.Value)
        {
            throw new ValidationException(_localizer["SalesQuoteOrderBusinessMismatch"]);
        }

        if (quote.CustomerId.HasValue && order.CustomerId.HasValue && quote.CustomerId.Value != order.CustomerId.Value)
        {
            throw new ValidationException(_localizer["SalesQuoteOrderCustomerMismatch"]);
        }

        quote.Status = SalesQuoteStatus.Converted;
        quote.ConvertedOrderId = order.Id;
        quote.ConvertedAtUtc = _clock.UtcNow;
        quote.ConvertedByUserId = dto.ActorUserId;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await _events.RecordStatusChangedAsync(quote, "sales.quote.converted", SalesQuoteStatus.Accepted, SalesQuoteStatus.Converted, dto.ActorUserId, dto.Reason, ct).ConfigureAwait(false);
    }

    private async Task TransitionAsync(
        SalesQuoteLifecycleDto dto,
        SalesQuoteStatus requiredStatus,
        SalesQuoteStatus targetStatus,
        string eventType,
        Action<SalesQuote> mutate,
        CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        var quote = await LoadQuoteAsync(dto.Id, ct).ConfigureAwait(false);
        SalesQuoteGuard.EnsureRowVersion(quote.RowVersion, dto.RowVersion, _localizer);
        if (quote.Status != requiredStatus)
        {
            throw new ValidationException(_localizer["SalesQuoteInvalidLifecycleTransition"]);
        }

        var from = quote.Status;
        quote.Status = targetStatus;
        mutate(quote);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await _events.RecordStatusChangedAsync(quote, eventType, from, targetStatus, dto.ActorUserId, dto.Reason, ct).ConfigureAwait(false);
    }

    private async Task<SalesQuote> LoadQuoteAsync(Guid id, CancellationToken ct)
    {
        var quote = await _db.Set<SalesQuote>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        return quote ?? throw new InvalidOperationException(_localizer["SalesQuoteNotFound"]);
    }
}

public sealed class ConvertSalesQuoteToOrderHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<SalesQuoteLifecycleDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly OrderCreationService _orderCreation;
    private readonly SalesQuoteLifecycleEventService _events;

    public ConvertSalesQuoteToOrderHandler(
        IAppDbContext db,
        IValidator<SalesQuoteLifecycleDto> validator,
        IStringLocalizer<ValidationResource> localizer,
        IClock clock,
        OrderCreationService orderCreation,
        SalesQuoteLifecycleEventService events)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _orderCreation = orderCreation ?? throw new ArgumentNullException(nameof(orderCreation));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public async Task<Guid> HandleAsync(SalesQuoteCreateOrderDto dto, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        var quote = await _db.Set<SalesQuote>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (quote is null)
        {
            throw new InvalidOperationException(_localizer["SalesQuoteNotFound"]);
        }

        SalesQuoteGuard.EnsureRowVersion(quote.RowVersion, dto.RowVersion, _localizer);
        if (quote.Status != SalesQuoteStatus.Accepted)
        {
            throw new ValidationException(_localizer["SalesQuoteOrderCreationRequiresAccepted"]);
        }

        if (quote.ConvertedOrderId.HasValue)
        {
            throw new ValidationException(_localizer["SalesQuoteAlreadyConverted"]);
        }

        var activeLines = quote.Lines
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAtUtc)
            .ToList();
        if (activeLines.Count == 0)
        {
            throw new ValidationException(_localizer["SalesQuoteRequiresLines"]);
        }

        var order = await _orderCreation.CreateAsync(new OrderCreationRequest
        {
            BusinessId = quote.BusinessId,
            CustomerId = quote.CustomerId,
            Currency = quote.Currency,
            PricesIncludeTax = false,
            SalesChannel = SalesChannel.Admin,
            BillingAddressJson = quote.BillingAddressJson,
            ShippingAddressJson = quote.ShippingAddressJson,
            InternalNotes = BuildOrderInternalNotes(quote),
            Lines = activeLines.Select(line => new OrderCreationLineRequest
            {
                VariantId = line.ProductVariantId,
                WarehouseId = null,
                Name = line.Name,
                Sku = line.Sku,
                Quantity = line.Quantity,
                UnitPriceNetMinor = line.UnitPriceNetMinor,
                VatRate = line.TaxRate,
                UnitPriceGrossMinor = line.UnitPriceGrossMinor,
                LineTaxMinor = line.TotalTaxMinor,
                LineGrossMinor = line.TotalGrossMinor
            }).ToList()
        }, ct).ConfigureAwait(false);

        quote.Status = SalesQuoteStatus.Converted;
        quote.ConvertedOrderId = order.Id;
        quote.ConvertedAtUtc = _clock.UtcNow;
        quote.ConvertedByUserId = dto.ActorUserId;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await _events.RecordOrderCreatedAsync(quote, order.Id, order.OrderNumber, dto.ActorUserId, dto.Reason, ct).ConfigureAwait(false);
        await _events.RecordStatusChangedAsync(quote, "sales.quote.converted", SalesQuoteStatus.Accepted, SalesQuoteStatus.Converted, dto.ActorUserId, dto.Reason, ct).ConfigureAwait(false);
        return order.Id;
    }

    private static string? BuildOrderInternalNotes(SalesQuote quote)
    {
        var quoteRef = string.IsNullOrWhiteSpace(quote.QuoteNumber) ? quote.Id.ToString("D") : quote.QuoteNumber;
        var prefix = $"Created from sales quote {quoteRef}.";
        return string.IsNullOrWhiteSpace(quote.InternalNotes)
            ? prefix
            : $"{prefix}{Environment.NewLine}{quote.InternalNotes.Trim()}";
    }
}

internal static class SalesQuoteMapper
{
    public static SalesQuote CreateEntity(SalesQuoteCreateDto dto)
    {
        var quote = new SalesQuote();
        UpdateEntity(quote, dto);
        return quote;
    }

    public static void UpdateEntity(SalesQuote quote, SalesQuoteCreateDto dto)
    {
        quote.BusinessId = NormalizeGuid(dto.BusinessId);
        quote.CustomerId = NormalizeGuid(dto.CustomerId);
        quote.OpportunityId = NormalizeGuid(dto.OpportunityId);
        quote.Title = dto.Title.Trim();
        quote.Currency = dto.Currency.Trim().ToUpperInvariant();
        quote.ValidUntilUtc = dto.ValidUntilUtc;
        quote.OwnerUserId = NormalizeGuid(dto.OwnerUserId);
        quote.PreparedByUserId = NormalizeGuid(dto.PreparedByUserId);
        quote.CustomerSnapshotJson = NormalizeJson(dto.CustomerSnapshotJson);
        quote.BillingAddressJson = NormalizeJson(dto.BillingAddressJson);
        quote.ShippingAddressJson = NormalizeJson(dto.ShippingAddressJson);
        quote.InternalNotes = NormalizeOptional(dto.InternalNotes);

        quote.Lines.Clear();
        var orderedLines = (dto.Lines ?? new List<SalesQuoteLineEditDto>())
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        for (var i = 0; i < orderedLines.Count; i++)
        {
            quote.Lines.Add(MapLine(orderedLines[i], i));
        }

        quote.TotalNetMinor = quote.Lines.Sum(x => x.TotalNetMinor);
        quote.TotalTaxMinor = quote.Lines.Sum(x => x.TotalTaxMinor);
        quote.TotalGrossMinor = quote.Lines.Sum(x => x.TotalGrossMinor);
    }

    private static SalesQuoteLine MapLine(SalesQuoteLineEditDto dto, int index)
    {
        var totalNet = dto.Quantity * dto.UnitPriceNetMinor;
        var totalGross = dto.Quantity * dto.UnitPriceGrossMinor;
        return new SalesQuoteLine
        {
            ProductVariantId = NormalizeGuid(dto.ProductVariantId),
            Name = dto.Name.Trim(),
            Sku = NormalizeOptional(dto.Sku),
            Description = NormalizeOptional(dto.Description),
            Quantity = dto.Quantity,
            UnitPriceNetMinor = dto.UnitPriceNetMinor,
            UnitPriceGrossMinor = dto.UnitPriceGrossMinor,
            TaxRate = dto.TaxRate,
            TotalNetMinor = totalNet,
            TotalGrossMinor = totalGross,
            TotalTaxMinor = Math.Max(0, totalGross - totalNet),
            SortOrder = dto.SortOrder == 0 ? index : dto.SortOrder
        };
    }

    private static Guid? NormalizeGuid(Guid? value) => value.HasValue && value.Value != Guid.Empty ? value : null;

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "{}";
        }

        using var _ = JsonDocument.Parse(value);
        return value.Trim();
    }
}

internal static class SalesQuoteGuard
{
    public static void EnsureRowVersion(byte[]? current, byte[]? submitted, IStringLocalizer<ValidationResource> localizer)
    {
        var submittedVersion = submitted ?? Array.Empty<byte>();
        if (submittedVersion.Length == 0 || !(current ?? Array.Empty<byte>()).SequenceEqual(submittedVersion))
        {
            throw new DbUpdateConcurrencyException(localizer["ConcurrencyConflictDetected"]);
        }
    }

    public static async Task ValidateLinksAsync(
        IAppDbContext db,
        Guid? businessId,
        Guid? customerId,
        Guid? opportunityId,
        IStringLocalizer<ValidationResource> localizer,
        CancellationToken ct)
    {
        businessId = NormalizeGuid(businessId);
        customerId = NormalizeGuid(customerId);
        opportunityId = NormalizeGuid(opportunityId);

        if (businessId.HasValue &&
            !await db.Set<Business>().AsNoTracking().AnyAsync(x => x.Id == businessId.Value && !x.IsDeleted, ct).ConfigureAwait(false))
        {
            throw new ValidationException(localizer["BusinessNotFound"]);
        }

        if (customerId.HasValue &&
            !await db.Set<Customer>().AsNoTracking().AnyAsync(x => x.Id == customerId.Value && !x.IsDeleted, ct).ConfigureAwait(false))
        {
            throw new ValidationException(localizer["CustomerNotFound"]);
        }

        if (opportunityId.HasValue)
        {
            var opportunity = await db.Set<Opportunity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == opportunityId.Value && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (opportunity is null)
            {
                throw new ValidationException(localizer["OpportunityNotFound"]);
            }

            if (customerId.HasValue && opportunity.CustomerId != customerId.Value)
            {
                throw new ValidationException(localizer["SalesQuoteOpportunityCustomerMismatch"]);
            }
        }
    }

    private static Guid? NormalizeGuid(Guid? value) => value.HasValue && value.Value != Guid.Empty ? value : null;
}
