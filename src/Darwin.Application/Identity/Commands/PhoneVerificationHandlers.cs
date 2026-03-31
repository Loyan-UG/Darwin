using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Settings;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands;

public sealed class RequestPhoneVerificationHandler
{
    private const string PhoneVerificationPurpose = "PhoneVerification";

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ISmsSender _smsSender;
    private readonly IWhatsAppSender _whatsAppSender;
    private readonly IClock _clock;
    private readonly IValidator<RequestPhoneVerificationDto> _validator;

    public RequestPhoneVerificationHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        ISmsSender smsSender,
        IWhatsAppSender whatsAppSender,
        IClock clock,
        IValidator<RequestPhoneVerificationDto> validator)
    {
        _db = db;
        _currentUser = currentUser;
        _smsSender = smsSender;
        _whatsAppSender = whatsAppSender;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(RequestPhoneVerificationDto dto, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(dto, ct);

        var userId = _currentUser.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Result.Fail("Unauthorized.");
        }

        var user = await _db.Set<User>()
            .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted && x.IsActive, ct);

        if (user is null)
        {
            return Result.Fail("User not found.");
        }

        if (string.IsNullOrWhiteSpace(user.PhoneE164))
        {
            return Result.Fail("Phone number is missing.");
        }

        var settings = await _db.Set<SiteSetting>().AsNoTracking().FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            return Result.Fail("Communication settings are missing.");
        }

        var utcNow = _clock.UtcNow;
        var expiresAtUtc = utcNow.AddMinutes(15);

        var activeTokens = await _db.Set<UserToken>()
            .Where(x => x.UserId == user.Id &&
                        x.Purpose == PhoneVerificationPurpose &&
                        x.UsedAtUtc == null)
            .ToListAsync(ct);

        foreach (var activeToken in activeTokens)
        {
            activeToken.MarkUsed(utcNow);
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        _db.Set<UserToken>().Add(new UserToken(user.Id, PhoneVerificationPurpose, code, expiresAtUtc));
        await _db.SaveChangesAsync(ct);

        var placeholders = new Dictionary<string, string?>
        {
            ["phone_e164"] = user.PhoneE164,
            ["token"] = code,
            ["expires_at_utc"] = expiresAtUtc.ToString("u")
        };

        var requestedChannel = dto.Channel;
        var preferredChannel = ParsePreferredChannel(settings.PhoneVerificationPreferredChannel);
        var effectiveChannel = requestedChannel ?? preferredChannel;

        if (effectiveChannel == PhoneVerificationChannel.Sms && IsSmsReady(settings))
        {
            var text = TransactionalEmailTemplateRenderer.Render(
                settings.PhoneVerificationSmsTemplate,
                "Your Darwin verification code is {token}. It expires at {expires_at_utc} UTC.",
                placeholders);
            await _smsSender.SendAsync(
                user.PhoneE164,
                text,
                ct,
                new ChannelDispatchContext
                {
                    FlowKey = "PhoneVerification"
                });
            return Result.Ok();
        }

        if (effectiveChannel == PhoneVerificationChannel.WhatsApp && IsWhatsAppReady(settings))
        {
            var whatsAppText = TransactionalEmailTemplateRenderer.Render(
                settings.PhoneVerificationWhatsAppTemplate,
                "Confirm your Darwin mobile number with code {token}. It expires at {expires_at_utc} UTC.",
                placeholders);
            await _whatsAppSender.SendTextAsync(
                user.PhoneE164,
                whatsAppText,
                ct,
                new ChannelDispatchContext
                {
                    FlowKey = "PhoneVerification"
                });
            return Result.Ok();
        }

        if (settings.PhoneVerificationAllowFallback)
        {
            var fallbackChannel = effectiveChannel == PhoneVerificationChannel.WhatsApp
                ? PhoneVerificationChannel.Sms
                : PhoneVerificationChannel.WhatsApp;

            if (fallbackChannel == PhoneVerificationChannel.Sms && IsSmsReady(settings))
            {
                var smsFallbackText = TransactionalEmailTemplateRenderer.Render(
                    settings.PhoneVerificationSmsTemplate,
                    "Your Darwin verification code is {token}. It expires at {expires_at_utc} UTC.",
                    placeholders);
                await _smsSender.SendAsync(
                    user.PhoneE164,
                    smsFallbackText,
                    ct,
                    new ChannelDispatchContext
                    {
                        FlowKey = "PhoneVerification"
                    });
                return Result.Ok();
            }

            if (fallbackChannel == PhoneVerificationChannel.WhatsApp && IsWhatsAppReady(settings))
            {
                var whatsAppFallbackText = TransactionalEmailTemplateRenderer.Render(
                    settings.PhoneVerificationWhatsAppTemplate,
                    "Confirm your Darwin mobile number with code {token}. It expires at {expires_at_utc} UTC.",
                    placeholders);
                await _whatsAppSender.SendTextAsync(
                    user.PhoneE164,
                    whatsAppFallbackText,
                    ct,
                    new ChannelDispatchContext
                    {
                        FlowKey = "PhoneVerification"
                    });
                return Result.Ok();
            }
        }

        return effectiveChannel == PhoneVerificationChannel.WhatsApp
            ? Result.Fail("WhatsApp verification is not available right now.")
            : Result.Fail("SMS verification is not available right now.");
    }

    private static PhoneVerificationChannel ParsePreferredChannel(string? configuredChannel)
    {
        return string.Equals(configuredChannel, "WhatsApp", StringComparison.OrdinalIgnoreCase)
            ? PhoneVerificationChannel.WhatsApp
            : PhoneVerificationChannel.Sms;
    }

    private static bool IsSmsReady(SiteSetting settings)
    {
        return settings.SmsEnabled &&
               !string.IsNullOrWhiteSpace(settings.SmsProvider) &&
               !string.IsNullOrWhiteSpace(settings.SmsFromPhoneE164);
    }

    private static bool IsWhatsAppReady(SiteSetting settings)
    {
        return settings.WhatsAppEnabled &&
               !string.IsNullOrWhiteSpace(settings.WhatsAppBusinessPhoneId) &&
               !string.IsNullOrWhiteSpace(settings.WhatsAppAccessToken);
    }
}

public sealed class ConfirmPhoneVerificationHandler
{
    private const string PhoneVerificationPurpose = "PhoneVerification";

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly IValidator<ConfirmPhoneVerificationDto> _validator;

    public ConfirmPhoneVerificationHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IClock clock,
        IValidator<ConfirmPhoneVerificationDto> validator)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(ConfirmPhoneVerificationDto dto, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(dto, ct);

        var userId = _currentUser.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Result.Fail("Unauthorized.");
        }

        var user = await _db.Set<User>()
            .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted && x.IsActive, ct);

        if (user is null)
        {
            return Result.Fail("User not found.");
        }

        if (string.IsNullOrWhiteSpace(user.PhoneE164))
        {
            return Result.Fail("Phone number is missing.");
        }

        var utcNow = _clock.UtcNow;
        var token = await _db.Set<UserToken>()
            .Where(x => x.UserId == user.Id &&
                        x.Purpose == PhoneVerificationPurpose &&
                        x.UsedAtUtc == null &&
                        x.Value == dto.Code)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (token is null)
        {
            return Result.Fail("Invalid or expired verification code.");
        }

        if (token.ExpiresAtUtc.HasValue && token.ExpiresAtUtc.Value < utcNow)
        {
            return Result.Fail("Invalid or expired verification code.");
        }

        user.PhoneNumberConfirmed = true;
        token.MarkUsed(utcNow);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
