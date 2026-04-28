using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Integration;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Commands
{
    public sealed class CancelCommunicationDispatchOperationHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public CancelCommunicationDispatchOperationHandler(
            IAppDbContext db,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(CancelCommunicationDispatchOperationDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            if (string.Equals(dto.Channel, "Email", StringComparison.OrdinalIgnoreCase))
            {
                return await CancelEmailOperationAsync(dto, ct).ConfigureAwait(false);
            }

            return await CancelChannelOperationAsync(dto, ct).ConfigureAwait(false);
        }

        public async Task<Result<int>> HandleChannelBatchAsync(CancelChannelDispatchOperationsBatchDto dto, CancellationToken ct = default)
        {
            if (dto is null)
            {
                return Result<int>.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var limit = dto.Limit <= 0 ? 200 : Math.Min(dto.Limit, 200);
            var query = _db.Set<ChannelDispatchOperation>()
                .Where(x => !x.IsDeleted)
                .Where(x => x.Status == "Pending" || x.Status == "Failed");

            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                var normalizedStatus = dto.Status.Trim();
                if (!string.Equals(normalizedStatus, "Pending", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(normalizedStatus, "Failed", StringComparison.OrdinalIgnoreCase))
                {
                    return Result<int>.Fail(_localizer["InvalidDeleteRequest"]);
                }

                query = query.Where(x => x.Status == normalizedStatus);
            }

            if (dto.FailedOnly)
            {
                query = query.Where(x => x.Status == "Failed");
            }

            if (dto.BusinessId.HasValue)
            {
                query = query.Where(x => x.BusinessId == dto.BusinessId.Value);
            }

            if (!string.IsNullOrWhiteSpace(dto.Query))
            {
                var q = dto.Query.Trim().ToLowerInvariant();
                query = query.Where(x =>
                    x.RecipientAddress.ToLower().Contains(q) ||
                    (x.IntendedRecipientAddress != null && x.IntendedRecipientAddress.ToLower().Contains(q)) ||
                    x.Provider.ToLower().Contains(q) ||
                    x.MessageText.ToLower().Contains(q) ||
                    (x.FlowKey != null && x.FlowKey.ToLower().Contains(q)) ||
                    (x.TemplateKey != null && x.TemplateKey.ToLower().Contains(q)) ||
                    (x.CorrelationKey != null && x.CorrelationKey.ToLower().Contains(q)) ||
                    (x.FailureReason != null && x.FailureReason.ToLower().Contains(q)));
            }

            if (!string.IsNullOrWhiteSpace(dto.RecipientAddress))
            {
                var normalizedRecipientAddress = dto.RecipientAddress.Trim();
                query = query.Where(x => (x.IntendedRecipientAddress ?? x.RecipientAddress) == normalizedRecipientAddress);
            }

            if (!string.IsNullOrWhiteSpace(dto.Channel))
            {
                var normalizedChannel = dto.Channel.Trim();
                query = query.Where(x => x.Channel == normalizedChannel);
            }

            if (!string.IsNullOrWhiteSpace(dto.Provider))
            {
                var normalizedProvider = dto.Provider.Trim();
                query = query.Where(x => x.Provider == normalizedProvider);
            }

            if (!string.IsNullOrWhiteSpace(dto.FlowKey))
            {
                var normalizedFlowKey = dto.FlowKey.Trim();
                query = query.Where(x => x.FlowKey == normalizedFlowKey);
            }

            if (dto.PhoneVerificationOnly)
            {
                query = query.Where(x => x.FlowKey == "PhoneVerification");
            }

            if (dto.AdminTestOnly)
            {
                query = query.Where(x => x.FlowKey == "AdminCommunicationTest");
            }

            var operations = await query
                .OrderByDescending(x => x.LastAttemptAtUtc ?? x.CreatedAtUtc)
                .Take(limit)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var operation in operations)
            {
                operation.IsDeleted = true;
            }

            if (operations.Count > 0)
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            return Result<int>.Ok(operations.Count);
        }

        private async Task<Result> CancelEmailOperationAsync(CancelCommunicationDispatchOperationDto dto, CancellationToken ct)
        {
            var operation = await _db.Set<EmailDispatchOperation>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (operation is null)
            {
                return Result.Fail(_localizer["CommunicationDispatchOperationNotFound"]);
            }

            return await CancelOperationAsync(operation, dto.RowVersion, ct).ConfigureAwait(false);
        }

        private async Task<Result> CancelChannelOperationAsync(CancelCommunicationDispatchOperationDto dto, CancellationToken ct)
        {
            var operation = await _db.Set<ChannelDispatchOperation>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (operation is null)
            {
                return Result.Fail(_localizer["CommunicationDispatchOperationNotFound"]);
            }

            return await CancelOperationAsync(operation, dto.RowVersion, ct).ConfigureAwait(false);
        }

        private async Task<Result> CancelOperationAsync(Domain.Common.BaseEntity operation, byte[] rowVersion, CancellationToken ct)
        {
            if (operation.IsDeleted)
            {
                return Result.Ok();
            }

            if (!operation.RowVersion.SequenceEqual(rowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            operation.IsDeleted = true;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Result.Ok();
        }
    }
}
