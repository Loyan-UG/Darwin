using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Returns a business-scoped communication profile used by WebAdmin support operators.
    /// </summary>
    public sealed class GetBusinessCommunicationProfileHandler
    {
        private readonly IAppDbContext _db;

        public GetBusinessCommunicationProfileHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<BusinessCommunicationProfileDto?> HandleAsync(Guid businessId, CancellationToken ct = default)
        {
            var nowUtc = DateTime.UtcNow;

            var business = await _db.Set<Business>()
                .AsNoTracking()
                .Where(x => x.Id == businessId)
                .Select(x => new BusinessCommunicationProfileDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    LegalName = x.LegalName,
                    ContactEmail = x.ContactEmail,
                    DefaultCulture = x.DefaultCulture,
                    DefaultTimeZoneId = x.DefaultTimeZoneId,
                    IsActive = x.IsActive,
                    OperationalStatus = x.OperationalStatus.ToString(),
                    SupportEmail = x.SupportEmail,
                    CommunicationSenderName = x.CommunicationSenderName,
                    CommunicationReplyToEmail = x.CommunicationReplyToEmail,
                    CustomerEmailNotificationsEnabled = x.CustomerEmailNotificationsEnabled,
                    CustomerMarketingEmailsEnabled = x.CustomerMarketingEmailsEnabled,
                    OperationalAlertEmailsEnabled = x.OperationalAlertEmailsEnabled,
                    MissingSupportEmail = string.IsNullOrWhiteSpace(x.SupportEmail),
                    MissingSenderIdentity = string.IsNullOrWhiteSpace(x.CommunicationSenderName) || string.IsNullOrWhiteSpace(x.CommunicationReplyToEmail),
                    OpenInvitationCount = x.Invitations.Count(i => i.Status == BusinessInvitationStatus.Pending || i.Status == BusinessInvitationStatus.Expired)
                })
                .SingleOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (business is null)
            {
                return null;
            }

            business.PendingActivationMemberCount = await (
                from member in _db.Set<BusinessMember>().AsNoTracking()
                join user in _db.Set<User>().AsNoTracking() on member.UserId equals user.Id into userJoin
                from user in userJoin.DefaultIfEmpty()
                where member.BusinessId == businessId && user != null && !user.EmailConfirmed
                select member.Id)
                .CountAsync(ct)
                .ConfigureAwait(false);

            business.LockedMemberCount = await (
                from member in _db.Set<BusinessMember>().AsNoTracking()
                join user in _db.Set<User>().AsNoTracking() on member.UserId equals user.Id into userJoin
                from user in userJoin.DefaultIfEmpty()
                where member.BusinessId == businessId &&
                      user != null &&
                      user.LockoutEndUtc.HasValue &&
                      user.LockoutEndUtc.Value > nowUtc
                select member.Id)
                .CountAsync(ct)
                .ConfigureAwait(false);

            return business;
        }
    }
}
