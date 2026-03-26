using System.Collections;
using System.Text.Json;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands;

/// <summary>
/// Updates the current authenticated user's privacy and communication preferences.
/// </summary>
public sealed class UpdateCurrentUserPreferencesHandler
{
    private const string EmailKey = "Email";
    private const string SmsKey = "SMS";
    private const string WhatsAppKey = "WhatsApp";
    private const string PushKey = "Push";
    private const string AnalyticsKey = "Analytics";

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<UpdateMemberPreferencesDto> _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCurrentUserPreferencesHandler"/> class.
    /// </summary>
    public UpdateCurrentUserPreferencesHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IValidator<UpdateMemberPreferencesDto> validator)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <summary>
    /// Updates the current user's preferences while preserving any unrelated channel keys already stored.
    /// </summary>
    public async Task<Result> HandleAsync(UpdateMemberPreferencesDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

        var userId = _currentUser.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Result.Fail("User is not authenticated.");
        }

        var user = await _db.Set<User>()
            .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted && x.IsActive, ct)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result.Fail("User not found.");
        }

        if (user.RowVersion is not null && user.RowVersion.Length > 0)
        {
            if (!StructuralComparisons.StructuralEqualityComparer.Equals(user.RowVersion, dto.RowVersion))
            {
                return Result.Fail("Concurrency conflict.");
            }
        }

        var channels = DeserializeChannels(user.ChannelsOptInJson);
        var marketingEnabled = dto.MarketingConsent;

        user.MarketingConsent = marketingEnabled;

        channels[EmailKey] = marketingEnabled && dto.AllowEmailMarketing;
        channels[SmsKey] = marketingEnabled && dto.AllowSmsMarketing;
        channels[WhatsAppKey] = marketingEnabled && dto.AllowWhatsAppMarketing;
        channels[PushKey] = marketingEnabled && dto.AllowPromotionalPushNotifications;
        channels[AnalyticsKey] = dto.AllowOptionalAnalyticsTracking;

        user.ChannelsOptInJson = JsonSerializer.Serialize(channels);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Ok();
    }

    private static Dictionary<string, bool> DeserializeChannels(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, bool>>(json)
                   ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
