using System.Text.Json;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Queries;

/// <summary>
/// Returns the current authenticated user's privacy and communication preferences.
/// </summary>
public sealed class GetCurrentUserPreferencesHandler
{
    private const string EmailKey = "Email";
    private const string SmsKey = "SMS";
    private const string WhatsAppKey = "WhatsApp";
    private const string PushKey = "Push";
    private const string AnalyticsKey = "Analytics";

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCurrentUserPreferencesHandler"/> class.
    /// </summary>
    public GetCurrentUserPreferencesHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>
    /// Loads the current user's preferences in a member-facing shape.
    /// </summary>
    public async Task<Result<MemberPreferencesDto>> HandleAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Result<MemberPreferencesDto>.Fail("User is not authenticated.");
        }

        var dto = await _db.Set<User>()
            .AsNoTracking()
            .Where(x => x.Id == userId && !x.IsDeleted && x.IsActive)
            .Select(x => new
            {
                x.RowVersion,
                x.MarketingConsent,
                x.ChannelsOptInJson,
                x.AcceptsTermsAtUtc
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (dto is null)
        {
            return Result<MemberPreferencesDto>.Fail("User not found.");
        }

        var channels = DeserializeChannels(dto.ChannelsOptInJson);

        return Result<MemberPreferencesDto>.Ok(new MemberPreferencesDto
        {
            RowVersion = dto.RowVersion ?? Array.Empty<byte>(),
            MarketingConsent = dto.MarketingConsent,
            AllowEmailMarketing = ReadFlag(channels, EmailKey),
            AllowSmsMarketing = ReadFlag(channels, SmsKey),
            AllowWhatsAppMarketing = ReadFlag(channels, WhatsAppKey),
            AllowPromotionalPushNotifications = ReadFlag(channels, PushKey),
            AllowOptionalAnalyticsTracking = ReadFlag(channels, AnalyticsKey),
            AcceptsTermsAtUtc = dto.AcceptsTermsAtUtc
        });
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

    private static bool ReadFlag(IReadOnlyDictionary<string, bool> channels, string key)
        => channels.TryGetValue(key, out var enabled) && enabled;
}
