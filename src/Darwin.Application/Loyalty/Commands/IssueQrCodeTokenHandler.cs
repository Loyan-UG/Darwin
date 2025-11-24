using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Validators;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Shared.Results;
using FluentValidation;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Issues a short-lived, opaque <see cref="QrCodeToken"/> for the consumer app.
    /// </summary>
    public sealed class IssueQrCodeTokenHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IssueQrCodeTokenValidator _validator = new();

        /// <summary>
        /// Initializes the handler.
        /// </summary>
        public IssueQrCodeTokenHandler(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Issues a token and returns its payload for QR presentation.
        /// </summary>
        public async Task<Result<QrCodeTokenIssuedDto>> HandleAsync(IssueQrCodeTokenDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid)
                return Result<QrCodeTokenIssuedDto>.Fail("Invalid token request.");

            var now = _clock.UtcNow;

            var entity = new QrCodeToken
            {
                UserId = dto.UserId,
                LoyaltyAccountId = dto.LoyaltyAccountId,
                Purpose = dto.Purpose,
                IssuedAtUtc = now,
                ExpiresAtUtc = now.AddSeconds(dto.TtlSeconds),
                IssuedDeviceId = dto.IssuedDeviceId,
                Token = GenerateOpaqueToken()
            };

            _db.Set<QrCodeToken>().Add(entity);
            await _db.SaveChangesAsync(ct);

            var resultDto = new QrCodeTokenIssuedDto
            {
                Id = entity.Id,
                Token = entity.Token,
                Purpose = entity.Purpose,
                IssuedAtUtc = entity.IssuedAtUtc,
                ExpiresAtUtc = entity.ExpiresAtUtc
            };

            return Result<QrCodeTokenIssuedDto>.Ok(resultDto);
        }

        /// <summary>
        /// Generates a URL-safe random token without PII.
        /// </summary>
        private static string GenerateOpaqueToken()
        {
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);

            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }
}
