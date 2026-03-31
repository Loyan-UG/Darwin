using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Commands
{
    /// <summary>
    /// Replays supported live email flows from the email-audit queue after resolving the current safe target.
    /// </summary>
    public sealed class RetryEmailDispatchAuditHandler
    {
        private readonly IAppDbContext _db;
        private readonly ResendBusinessInvitationHandler _resendBusinessInvitation;
        private readonly RequestEmailConfirmationHandler _requestEmailConfirmation;
        private readonly RequestPasswordResetHandler _requestPasswordReset;

        public RetryEmailDispatchAuditHandler(
            IAppDbContext db,
            ResendBusinessInvitationHandler resendBusinessInvitation,
            RequestEmailConfirmationHandler requestEmailConfirmation,
            RequestPasswordResetHandler requestPasswordReset)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _resendBusinessInvitation = resendBusinessInvitation ?? throw new ArgumentNullException(nameof(resendBusinessInvitation));
            _requestEmailConfirmation = requestEmailConfirmation ?? throw new ArgumentNullException(nameof(requestEmailConfirmation));
            _requestPasswordReset = requestPasswordReset ?? throw new ArgumentNullException(nameof(requestPasswordReset));
        }

        public async Task<Result> HandleAsync(RetryEmailDispatchAuditDto dto, CancellationToken ct = default)
        {
            var audit = await _db.Set<EmailDispatchAudit>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == dto.AuditId, ct)
                .ConfigureAwait(false);

            if (audit is null)
            {
                return Result.Fail("Email audit row was not found.");
            }

            if (!CanRetryStatus(audit.Status))
            {
                return Result.Fail("Only failed or still-pending email audit rows can be retried.");
            }

            if (string.IsNullOrWhiteSpace(audit.RecipientEmail))
            {
                return Result.Fail("This audit row does not have a recipient email to resolve for retry.");
            }

            try
            {
                return audit.FlowKey switch
                {
                    "BusinessInvitation" => await RetryBusinessInvitationAsync(audit, ct).ConfigureAwait(false),
                    "AccountActivation" => await RetryAccountActivationAsync(audit, ct).ConfigureAwait(false),
                    "PasswordReset" => await RetryPasswordResetAsync(audit, ct).ConfigureAwait(false),
                    _ => Result.Fail("This audit flow does not support generic retry from the audit queue.")
                };
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message);
            }
        }

        private async Task<Result> RetryBusinessInvitationAsync(EmailDispatchAudit audit, CancellationToken ct)
        {
            if (!audit.BusinessId.HasValue)
            {
                return Result.Fail("Invitation retry requires a business-linked audit row.");
            }

            var normalizedEmail = audit.RecipientEmail.Trim().ToUpperInvariant();
            var invitation = await _db.Set<BusinessInvitation>()
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(
                    x => x.BusinessId == audit.BusinessId.Value &&
                         x.NormalizedEmail == normalizedEmail &&
                         !x.IsDeleted &&
                         x.Status != BusinessInvitationStatus.Accepted &&
                         x.Status != BusinessInvitationStatus.Revoked,
                    ct)
                .ConfigureAwait(false);

            if (invitation is null)
            {
                return Result.Fail("No open or expired invitation could be resolved for this recipient.");
            }

            await _resendBusinessInvitation.HandleAsync(
                new BusinessInvitationResendDto
                {
                    Id = invitation.Id,
                    ExpiresInDays = 7
                },
                ct).ConfigureAwait(false);

            return Result.Ok();
        }

        private async Task<Result> RetryAccountActivationAsync(EmailDispatchAudit audit, CancellationToken ct)
        {
            var user = await ResolveUserByEmailAsync(audit.RecipientEmail, ct).ConfigureAwait(false);
            if (user is null)
            {
                return Result.Fail("No active user could be resolved for this activation email.");
            }

            return await _requestEmailConfirmation.HandleAsync(
                new RequestEmailConfirmationDto
                {
                    Email = user.Email
                },
                ct).ConfigureAwait(false);
        }

        private async Task<Result> RetryPasswordResetAsync(EmailDispatchAudit audit, CancellationToken ct)
        {
            var user = await ResolveUserByEmailAsync(audit.RecipientEmail, ct).ConfigureAwait(false);
            if (user is null)
            {
                return Result.Fail("No active user could be resolved for this password reset email.");
            }

            return await _requestPasswordReset.HandleAsync(
                new RequestPasswordResetDto
                {
                    Email = user.Email
                },
                ct).ConfigureAwait(false);
        }

        private async Task<User?> ResolveUserByEmailAsync(string email, CancellationToken ct)
        {
            var normalizedEmail = email.Trim().ToUpperInvariant();
            return await _db.Set<User>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail && !x.IsDeleted, ct)
                .ConfigureAwait(false);
        }

        private static bool CanRetryStatus(string? status)
        {
            return string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase);
        }
    }
}
