using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Loads one business membership for editing, including user context for admin screens.
    /// </summary>
    public sealed class GetBusinessMemberForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetBusinessMemberForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<BusinessMemberDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return (
                from member in _db.Set<BusinessMember>().AsNoTracking()
                join user in _db.Set<User>().AsNoTracking() on member.UserId equals user.Id into userJoin
                from user in userJoin.DefaultIfEmpty()
                where member.Id == id
                select new BusinessMemberDetailDto
                {
                    Id = member.Id,
                    BusinessId = member.BusinessId,
                    UserId = member.UserId,
                    UserDisplayName = user == null
                        ? "Deleted user"
                        : string.IsNullOrWhiteSpace(((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim())
                            ? user.Email
                            : ((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim(),
                    UserEmail = user == null ? string.Empty : user.Email,
                    Role = member.Role,
                    IsActive = member.IsActive,
                    RowVersion = member.RowVersion
                })
                .FirstOrDefaultAsync(ct);
        }
    }
}
