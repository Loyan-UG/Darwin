using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds Identity-related fixtures (admin + multiple test users).
    /// Note: Passwords are not set here; use your flows or set a default hash in dev only.
    /// </summary>
    public sealed class IdentitySeedSection
    {
        /// <summary>
        /// Creates an admin and several demo users if they do not exist.
        /// Idempotent by email.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            // Admin
            var adminEmail = "admin@darwin.de";
            var admin = await db.Set<User>().FirstOrDefaultAsync(u => u.Email == adminEmail && !u.IsDeleted, ct);
            if (admin is null)
            {
                admin = new User(
                    email: adminEmail,
                    passwordHash: string.Empty, // set via your flows or test-only
                    securityStamp: Guid.NewGuid().ToString("N"))
                {
                    FirstName = "System",
                    LastName = "Admin",
                    IsSystem = true,
                    IsActive = true,
                    Locale = "de-DE",
                    Timezone = "Europe/Berlin",
                    Currency = "EUR"
                };
                db.Add(admin);
            }

            // A handful of demo customers
            var demoUsers = new (string Email, string First, string Last)[]
            {
                ("alice@darwin.com", "Alice", "Müller"),
                ("bob@darwin.com", "Bob", "Schmidt"),
                ("carol@darwin.com", "Carol", "Fischer"),
                ("dave@darwin.com", "Dave", "Wagner"),
                ("erin@darwin.com", "Erin", "Weber"),
                ("frank@darwin.com", "Frank", "Becker"),
            };

            foreach (var (email, first, last) in demoUsers)
            {
                var u = await db.Set<User>().FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted, ct);
                if (u is null)
                {
                    u = new User(
                        email: email,
                        passwordHash: string.Empty, // set via your flows or test-only
                        securityStamp: Guid.NewGuid().ToString("N"))
                    {
                        FirstName = first,
                        LastName = last,
                        IsActive = true,
                        Locale = "de-DE",
                        Timezone = "Europe/Berlin",
                        Currency = "EUR"
                    };
                    db.Add(u);
                }
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
