using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application;
using Darwin.Application.CRM.Services;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.CRM.Commands
{
    /// <summary>
    /// Appends a new interaction to a CRM record timeline.
    /// </summary>
    public sealed class CreateInteractionHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<InteractionCreateDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public CreateInteractionHandler(IAppDbContext db, IValidator<InteractionCreateDto> validator, IStringLocalizer<ValidationResource>? localizer = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
        }

        public async Task<Guid> HandleAsync(InteractionCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            if (dto.CustomerId.HasValue)
            {
                var customerExists = await _db.Set<Customer>()
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == dto.CustomerId.Value, ct)
                    .ConfigureAwait(false);

                if (!customerExists)
                {
                    throw new InvalidOperationException(_localizer["CustomerNotFound"]);
                }
            }

            if (dto.LeadId.HasValue)
            {
                var leadExists = await _db.Set<Lead>()
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == dto.LeadId.Value, ct)
                    .ConfigureAwait(false);

                if (!leadExists)
                {
                    throw new InvalidOperationException(_localizer["LeadNotFound"]);
                }
            }

            if (dto.OpportunityId.HasValue)
            {
                var opportunityExists = await _db.Set<Opportunity>()
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == dto.OpportunityId.Value, ct)
                    .ConfigureAwait(false);

                if (!opportunityExists)
                {
                    throw new InvalidOperationException(_localizer["OpportunityNotFound"]);
                }
            }

            var interaction = new Interaction
            {
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                OpportunityId = dto.OpportunityId,
                Type = dto.Type,
                Channel = dto.Channel,
                Subject = NormalizeOptional(dto.Subject),
                Content = NormalizeOptional(dto.Content),
                UserId = dto.UserId
            };

            _db.Set<Interaction>().Add(interaction);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return interaction.Id;
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Records a new consent decision for a CRM customer.
    /// </summary>
    public sealed class CreateConsentHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<ConsentCreateDto> _validator;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private readonly CrmFoundationPrimitiveService? _foundation;

        public CreateConsentHandler(
            IAppDbContext db,
            IValidator<ConsentCreateDto> validator,
            IStringLocalizer<ValidationResource>? localizer = null,
            IClock? clock = null,
            CrmFoundationPrimitiveService? foundation = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
            _foundation = foundation;
        }

        public async Task<Guid> HandleAsync(ConsentCreateDto dto, CancellationToken ct = default)
        {
            var nowUtc = _clock.UtcNow;
            if (dto.GrantedAtUtc == default)
            {
                dto.GrantedAtUtc = nowUtc;
            }

            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var customerExists = await _db.Set<Customer>()
                .AsNoTracking()
                .AnyAsync(x => x.Id == dto.CustomerId, ct)
                .ConfigureAwait(false);

            if (!customerExists)
            {
                throw new InvalidOperationException(_localizer["CustomerNotFound"]);
            }

            var consent = new Consent
            {
                CustomerId = dto.CustomerId,
                Type = dto.Type,
                Granted = dto.Granted,
                GrantedAtUtc = dto.GrantedAtUtc,
                RevokedAtUtc = dto.Granted ? null : dto.RevokedAtUtc ?? dto.GrantedAtUtc,
                Source = CrmEngagementHandlerHelpers.NormalizeOptional(dto.Source),
                PolicyVersion = CrmEngagementHandlerHelpers.NormalizeOptional(dto.PolicyVersion),
                EvidenceJson = CrmEngagementHandlerHelpers.NormalizeOptional(dto.EvidenceJson)
            };

            _db.Set<Consent>().Add(consent);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            await RecordConsentAsync(consent, ct).ConfigureAwait(false);
            return consent.Id;
        }

        private async Task RecordConsentAsync(Consent consent, CancellationToken ct)
        {
            if (_foundation is null)
            {
                return;
            }

            var eventType = consent.Granted ? "crm.consent.granted" : "crm.consent.revoked";
            var occurredAtUtc = consent.Granted ? consent.GrantedAtUtc : consent.RevokedAtUtc ?? consent.GrantedAtUtc;
            var eventKey = $"{eventType}:{consent.Id:N}";
            var payloadJson = $$"""{"consentId":"{{consent.Id}}","customerId":"{{consent.CustomerId}}","type":"{{consent.Type}}","granted":{{consent.Granted.ToString().ToLowerInvariant()}}}""";
            var eventResult = await _foundation.RecordLifecycleEventAsync(
                CrmFoundationPrimitiveService.EntityTypes.Consent,
                consent.Id,
                eventType,
                eventKey,
                occurredAtUtc,
                actorUserId: null,
                consent.Granted ? "Consent granted" : "Consent revoked",
                $"Customer id: {consent.CustomerId}",
                payloadJson,
                AuditTrailAction.StatusChanged,
                consent.PolicyVersion,
                ct)
                .ConfigureAwait(false);
            if (!eventResult.Succeeded)
            {
                throw new InvalidOperationException(eventResult.Error);
            }

            var activityResult = await _foundation.AddActivityAsync(
                CrmFoundationPrimitiveService.EntityTypes.Customer,
                consent.CustomerId,
                eventType,
                occurredAtUtc,
                actorUserId: null,
                consent.Granted ? "Consent granted" : "Consent revoked",
                $"Consent type: {consent.Type}",
                metadataJson: payloadJson,
                ct: ct)
                .ConfigureAwait(false);
            if (!activityResult.Succeeded)
            {
                throw new InvalidOperationException(activityResult.Error);
            }
        }
    }

    /// <summary>
    /// Creates a CRM customer segment definition.
    /// </summary>
    public sealed class CreateCustomerSegmentHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<CustomerSegmentEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public CreateCustomerSegmentHandler(IAppDbContext db, IValidator<CustomerSegmentEditDto> validator, IStringLocalizer<ValidationResource>? localizer = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
        }

        public async Task<Guid> HandleAsync(CustomerSegmentEditDto dto, CancellationToken ct = default)
        {
            dto.Id = Guid.Empty;
            dto.RowVersion = Array.Empty<byte>();
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var normalizedName = dto.Name.Trim();
            var normalizedCode = CrmEngagementHandlerHelpers.NormalizeCode(dto.Code, normalizedName);
            var exists = await _db.Set<CustomerSegment>()
                .AsNoTracking()
                .AnyAsync(x => x.Name == normalizedName || x.Code == normalizedCode, ct)
                .ConfigureAwait(false);

            if (exists)
            {
                throw new InvalidOperationException(_localizer["CustomerSegmentNameAlreadyExists"]);
            }

            var segment = new CustomerSegment
            {
                Name = normalizedName,
                Description = CrmEngagementHandlerHelpers.NormalizeOptional(dto.Description),
                Code = normalizedCode,
                IsActive = dto.IsActive,
                RuleJson = CrmEngagementHandlerHelpers.NormalizeOptional(dto.RuleJson)
            };

            _db.Set<CustomerSegment>().Add(segment);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return segment.Id;
        }
    }

    /// <summary>
    /// Updates a CRM customer segment definition.
    /// </summary>
    public sealed class UpdateCustomerSegmentHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<CustomerSegmentEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateCustomerSegmentHandler(IAppDbContext db, IValidator<CustomerSegmentEditDto> validator, IStringLocalizer<ValidationResource>? localizer = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
        }

        public async Task HandleAsync(CustomerSegmentEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var segment = await _db.Set<CustomerSegment>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (segment is null)
            {
                throw new InvalidOperationException(_localizer["CustomerSegmentNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = segment.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            var normalizedName = dto.Name.Trim();
            var normalizedCode = CrmEngagementHandlerHelpers.NormalizeCode(dto.Code, normalizedName);
            var exists = await _db.Set<CustomerSegment>()
                .AsNoTracking()
                .AnyAsync(x => x.Id != dto.Id && (x.Name == normalizedName || x.Code == normalizedCode), ct)
                .ConfigureAwait(false);

            if (exists)
            {
                throw new InvalidOperationException(_localizer["CustomerSegmentNameAlreadyExists"]);
            }

            segment.Name = normalizedName;
            segment.Description = CrmEngagementHandlerHelpers.NormalizeOptional(dto.Description);
            segment.Code = normalizedCode;
            segment.IsActive = dto.IsActive;
            segment.RuleJson = CrmEngagementHandlerHelpers.NormalizeOptional(dto.RuleJson);
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

    /// <summary>
    /// Assigns a CRM customer to a segment.
    /// </summary>
    public sealed class AssignCustomerSegmentHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<AssignCustomerSegmentDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public AssignCustomerSegmentHandler(IAppDbContext db, IValidator<AssignCustomerSegmentDto> validator, IStringLocalizer<ValidationResource>? localizer = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
        }

        public async Task<Guid> HandleAsync(AssignCustomerSegmentDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var customerExists = await _db.Set<Customer>()
                .AsNoTracking()
                .AnyAsync(x => x.Id == dto.CustomerId, ct)
                .ConfigureAwait(false);

            if (!customerExists)
            {
                throw new InvalidOperationException(_localizer["CustomerNotFound"]);
            }

            var segmentExists = await _db.Set<CustomerSegment>()
                .AsNoTracking()
                .AnyAsync(x => x.Id == dto.CustomerSegmentId, ct)
                .ConfigureAwait(false);

            if (!segmentExists)
            {
                throw new InvalidOperationException(_localizer["CustomerSegmentNotFound"]);
            }

            var exists = await _db.Set<CustomerSegmentMembership>()
                .AsNoTracking()
                .AnyAsync(x => x.CustomerId == dto.CustomerId && x.CustomerSegmentId == dto.CustomerSegmentId, ct)
                .ConfigureAwait(false);

            if (exists)
            {
                throw new InvalidOperationException(_localizer["CustomerAlreadyAssignedToSegment"]);
            }

            var membership = new CustomerSegmentMembership
            {
                CustomerId = dto.CustomerId,
                CustomerSegmentId = dto.CustomerSegmentId
            };

            _db.Set<CustomerSegmentMembership>().Add(membership);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return membership.Id;
        }
    }

    /// <summary>
    /// Removes a CRM customer from a segment.
    /// </summary>
    public sealed class RemoveCustomerSegmentMembershipHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public RemoveCustomerSegmentMembershipHandler(IAppDbContext db, IStringLocalizer<ValidationResource>? localizer = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
        }

        public async Task HandleAsync(Guid membershipId, CancellationToken ct = default)
        {
            var membership = await _db.Set<CustomerSegmentMembership>()
                .FirstOrDefaultAsync(x => x.Id == membershipId, ct)
                .ConfigureAwait(false);

            if (membership is null)
            {
                throw new InvalidOperationException(_localizer["CustomerSegmentMembershipNotFound"]);
            }

            _db.Set<CustomerSegmentMembership>().Remove(membership);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    internal static class CrmEngagementHandlerHelpers
    {
        public static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        public static string NormalizeCode(string? code, string name)
        {
            var source = string.IsNullOrWhiteSpace(code) ? name : code;
            var chars = source
                .Trim()
                .ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
                .ToArray();
            var normalized = string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
            return string.IsNullOrWhiteSpace(normalized) ? "segment" : normalized;
        }
    }
}

