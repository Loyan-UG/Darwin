using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Billing.Commands
{
public sealed class UpdatePaymentDisputeReviewHandler
    {
        public const string UnderReviewAction = "UnderReview";
        public const string EvidenceSubmittedAction = "EvidenceSubmitted";
        public const string ResolveWonAction = "ResolveWon";
        public const string ResolveLostAction = "ResolveLost";
        public const string ClearAction = "Clear";

        private const string MarkerPrefix = "[DisputeReview:";
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdatePaymentDisputeReviewHandler(
            IAppDbContext db,
            IStringLocalizer<ValidationResource>? localizer = null, IClock? clock = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
        }

        public async Task<Result> HandleAsync(UpdatePaymentDisputeReviewDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0)
            {
                return Result.Fail(_localizer["RowVersionRequired"]);
            }

            var payment = await _db.Set<Payment>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (payment is null)
            {
                return Result.Fail(_localizer["PaymentNotFound"]);
            }

            var currentVersion = payment.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(rowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            var nowUtc = _clock.UtcNow;
            if (!TryApplyAction(payment, dto.Action, dto.Note, nowUtc))
            {
                return Result.Fail(_localizer["PaymentDisputeReviewUnsupportedAction"]);
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            return Result.Ok();
        }

        private static bool TryApplyAction(Payment payment, string action, string? note, DateTime nowUtc)
        {
            var normalizedAction = NormalizeAction(action);
            if (string.IsNullOrWhiteSpace(normalizedAction))
            {
                return false;
            }

            if (string.Equals(normalizedAction, ClearAction, StringComparison.OrdinalIgnoreCase))
            {
                payment.FailureReason = RemoveExistingMarker(payment.FailureReason);
                return true;
            }

            var state = normalizedAction switch
            {
                var current when string.Equals(current, UnderReviewAction, StringComparison.OrdinalIgnoreCase) => "UnderReview",
                var current when string.Equals(current, EvidenceSubmittedAction, StringComparison.OrdinalIgnoreCase) => "EvidenceSubmitted",
                var current when string.Equals(current, ResolveWonAction, StringComparison.OrdinalIgnoreCase) => "Won",
                var current when string.Equals(current, ResolveLostAction, StringComparison.OrdinalIgnoreCase) => "Lost",
                _ => null
            };

            if (state is null)
            {
                return false;
            }

            if (string.Equals(normalizedAction, ResolveWonAction, StringComparison.OrdinalIgnoreCase) &&
                payment.Status == PaymentStatus.Failed)
            {
                payment.Status = PaymentStatus.Completed;
            }
            else if (string.Equals(normalizedAction, ResolveLostAction, StringComparison.OrdinalIgnoreCase) &&
                     payment.Status != PaymentStatus.Refunded)
            {
                payment.Status = PaymentStatus.Failed;
            }

            payment.FailureReason = BuildFailureReason(payment.FailureReason, state, note, nowUtc);
            return true;
        }

        private static string? BuildFailureReason(string? existing, string state, string? note, DateTime nowUtc)
        {
            var preserved = RemoveExistingMarker(existing);
            var marker = $"{MarkerPrefix}{state};{nowUtc:O};{NormalizeNote(note)}]";
            return string.IsNullOrWhiteSpace(preserved) ? marker : $"{preserved.Trim()} {marker}";
        }

        private static string NormalizeAction(string? action) =>
            string.IsNullOrWhiteSpace(action) ? string.Empty : action.Trim();

    private static string NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return "-";
        }

        return note.Trim()
            .Replace("[", "(")
            .Replace("]", ")")
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ");
    }

        public static string ResolveDisputeReviewState(string? failureReason)
        {
            if (string.IsNullOrWhiteSpace(failureReason))
            {
                return string.Empty;
            }

            var markerStart = failureReason.LastIndexOf(MarkerPrefix, StringComparison.Ordinal);
            if (markerStart < 0)
            {
                return string.Empty;
            }

            var stateStart = markerStart + MarkerPrefix.Length;
            var stateEnd = failureReason.IndexOf(';', stateStart);
            if (stateEnd < 0)
            {
                stateEnd = failureReason.IndexOf(']', stateStart);
            }

            return stateEnd > stateStart
                ? failureReason[stateStart..stateEnd]
                : string.Empty;
        }

        public static bool IsDisputeReviewResolved(string? failureReason)
        {
            var state = ResolveDisputeReviewState(failureReason);
            return string.Equals(state, "Won", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(state, "Lost", StringComparison.OrdinalIgnoreCase);
        }

        private static string? RemoveExistingMarker(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var markerStart = value.LastIndexOf(MarkerPrefix, StringComparison.Ordinal);
            if (markerStart < 0)
            {
                return value.Trim();
            }

            var markerEnd = value.IndexOf(']', markerStart);
            if (markerEnd < 0)
            {
                return value[..markerStart].Trim();
            }

            return (value[..markerStart] + value[(markerEnd + 1)..]).Trim();
        }
    }
}

