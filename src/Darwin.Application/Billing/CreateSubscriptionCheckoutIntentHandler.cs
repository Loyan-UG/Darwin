using Darwin.Application.Abstractions.Payments;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Billing;

/// <summary>
/// Validates and creates provider-backed checkout intents for subscription upgrades.
/// </summary>
public sealed class CreateSubscriptionCheckoutIntentHandler
{
    private readonly IAppDbContext _db;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly ISubscriptionCheckoutSessionClient? _subscriptionCheckoutSessionClient;
    private readonly IClock _clock;

    public CreateSubscriptionCheckoutIntentHandler(
        IAppDbContext db,
        IStringLocalizer<ValidationResource> localizer,
        ISubscriptionCheckoutSessionClient? subscriptionCheckoutSessionClient = null,
        IClock? clock = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _subscriptionCheckoutSessionClient = subscriptionCheckoutSessionClient;
        _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
    }

    public async Task<Result> ValidateAsync(Guid businessId, Guid planId, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty || planId == Guid.Empty)
        {
            return Result.Fail(_localizer["BusinessAndPlanIdentifiersRequired"]);
        }

        var hasPlan = await _db.Set<BillingPlan>()
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.IsActive && x.Id == planId, ct)
            .ConfigureAwait(false);

        if (!hasPlan)
        {
            return Result.Fail(_localizer["SelectedBillingPlanUnavailable"]);
        }

        var hasBusiness = await _db.Set<Business>()
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.Id == businessId, ct)
            .ConfigureAwait(false);

        if (!hasBusiness)
        {
            return Result.Fail(_localizer["BusinessNotFound"]);
        }

        return Result.Ok();
    }

    public async Task<Result<SubscriptionCheckoutIntentDto>> CreateAsync(
        Guid businessId,
        Guid planId,
        string successUrl,
        string cancelUrl,
        CancellationToken ct = default)
    {
        if (_subscriptionCheckoutSessionClient is null)
        {
            return Result<SubscriptionCheckoutIntentDto>.Fail(_localizer["StripeSubscriptionCheckoutProviderNotConfigured"]);
        }

        if (!Uri.TryCreate(successUrl, UriKind.Absolute, out _) || !Uri.TryCreate(cancelUrl, UriKind.Absolute, out _))
        {
            return Result<SubscriptionCheckoutIntentDto>.Fail(_localizer["BillingCheckoutEndpointNotConfigured"]);
        }

        var data = await (
                from business in _db.Set<Business>().AsNoTracking()
                from plan in _db.Set<BillingPlan>().AsNoTracking()
                where !business.IsDeleted &&
                      business.Id == businessId &&
                      !plan.IsDeleted &&
                      plan.IsActive &&
                      plan.Id == planId
                select new
                {
                    Business = business,
                    Plan = plan
                })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (data is null)
        {
            var validation = await ValidateAsync(businessId, planId, ct).ConfigureAwait(false);
            return validation.Succeeded
                ? Result<SubscriptionCheckoutIntentDto>.Fail(_localizer["CheckoutIntentCreationFailed"])
                : Result<SubscriptionCheckoutIntentDto>.Fail(validation.Error ?? _localizer["CheckoutIntentCreationFailed"]);
        }

        var settings = await _db.Set<SiteSetting>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.ModifiedAtUtc ?? x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (settings is null || !settings.StripeEnabled || string.IsNullOrWhiteSpace(settings.StripeSecretKey))
        {
            return Result<SubscriptionCheckoutIntentDto>.Fail(_localizer["StripeSubscriptionCheckoutProviderNotConfigured"]);
        }

        if (data.Plan.PriceMinor <= 0)
        {
            return Result<SubscriptionCheckoutIntentDto>.Fail(_localizer["SelectedBillingPlanUnavailable"]);
        }

        try
        {
            var providerResult = await _subscriptionCheckoutSessionClient.CreateSessionAsync(new SubscriptionCheckoutSessionRequest
            {
                Provider = "Stripe",
                SecretKey = settings.StripeSecretKey,
                BusinessId = data.Business.Id,
                BusinessName = ResolveBusinessName(data.Business),
                CustomerEmail = NormalizeOptional(data.Business.ContactEmail) ?? NormalizeOptional(data.Business.SupportEmail),
                PlanId = data.Plan.Id,
                PlanCode = data.Plan.Code,
                PlanName = data.Plan.Name,
                AmountMinor = data.Plan.PriceMinor,
                Currency = data.Plan.Currency,
                Interval = MapInterval(data.Plan.Interval),
                IntervalCount = data.Plan.IntervalCount,
                TrialDays = data.Plan.TrialDays,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            }, ct).ConfigureAwait(false);

            return Result<SubscriptionCheckoutIntentDto>.Ok(new SubscriptionCheckoutIntentDto
            {
                CheckoutUrl = providerResult.CheckoutUrl,
                ExpiresAtUtc = providerResult.ExpiresAtUtc ?? _clock.UtcNow.AddMinutes(30),
                Provider = "Stripe",
                ProviderCheckoutSessionReference = providerResult.ProviderCheckoutSessionReference,
                ProviderSubscriptionReference = providerResult.ProviderSubscriptionReference
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
        {
            return Result<SubscriptionCheckoutIntentDto>.Fail(_localizer["CheckoutIntentCreationFailed"]);
        }
    }

    private static string ResolveBusinessName(Business business)
        => NormalizeOptional(business.BrandDisplayName)
           ?? NormalizeOptional(business.LegalName)
           ?? NormalizeOptional(business.Name)
           ?? "Business subscription";

    private static string MapInterval(BillingInterval interval)
        => interval switch
        {
            BillingInterval.Day => "day",
            BillingInterval.Week => "week",
            BillingInterval.Year => "year",
            _ => "month"
        };

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class SubscriptionCheckoutIntentDto
{
    public string CheckoutUrl { get; init; } = string.Empty;

    public DateTime ExpiresAtUtc { get; init; }

    public string Provider { get; init; } = "Stripe";

    public string? ProviderCheckoutSessionReference { get; init; }

    public string? ProviderSubscriptionReference { get; init; }
}
