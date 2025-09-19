using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Identity.Services
{
    /// <summary>
    /// Centralized permission evaluation. Application handlers can call this to check an authenticated
    /// user's permissions abstractly. Infrastructure will build the user's effective permissions from
    /// Roles + direct grants (if any).
    /// </summary>
    public interface IPermissionService
    {
        Task<bool> HasAsync(Guid userId, string permissionKey, CancellationToken ct = default);
        Task<HashSet<string>> GetAllAsync(Guid userId, CancellationToken ct = default);
    }
}
