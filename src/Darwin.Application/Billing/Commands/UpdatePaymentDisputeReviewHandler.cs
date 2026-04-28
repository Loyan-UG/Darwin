using Darwin.Application.Abstractions.Persistence;
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
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdatePaymentDisputeReviewHandler(
            IAppDbContext db,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(UpdatePaymentDisputeReviewDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var payment = await _db.Set<Payment>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (payment is null)
            {
                return Result.Fail(_localizer["PaymentNotFound"]);
            }

            if (!payment.RowVersion.SequenceEqual(dto.RowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflict"]);
            }

            if (!TryApplyAction(payment, dto.Action, dto.Note))
            {
                return Result.Fail(_localizer["PaymentDisputeReviewUnsupportedAction"]);
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Result.Ok();
        }

        private static bool TryApplyAction(Payment payment, string action, string? note)
        {
            var normalizedAction = NormalizeAction(action);
            if (string.IsNullOrWhiteSpace(normalizedAction))
            {
                return false;
            }

            if (normalizedAction == ClearAction)
            {
                payment.FailureReason = RemoveExistingMarker(payment.FailureReason);
                return true;
            }

            var state = normalizedAction switch
            {
                UnderReviewAction => "UnderReview",
                EvidenceSubmittedAction => "EvidenceSubmitted",
                ResolveWonAction => "Won",
                ResolveLostAction => "Lost",
                _ => null
            };

            if (state is null)
            {
                return false;
            }

            if (normalizedAction == ResolveWonAction && payment.Status == PaymentStatus.Failed)
            {
                payment.Status = PaymentStatus.Completed;
            }
            else if (normalizedAction == ResolveLostAction && payment.Status != PaymentStatus.Refunded)
            {
                payment.Status = PaymentStatus.Failed;
            }

            payment.FailureReason = BuildFailureReason(payment.FailureReason, state, note, DateTime.UtcNow);
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

            return note.Trim().Replace("[", "(").Replace("]", ")").Replace(Environment.NewLine, " ");
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
