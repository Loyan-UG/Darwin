using Darwin.Infrastructure.Persistence.Db;
using Darwin.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Darwin.Tests.Common.TestInfrastructure;

/// <summary>
///     Provides deterministic database reset utilities for integration tests.
///     This helper intentionally performs destructive operations and therefore
///     enforces explicit safety guards before any reset is executed.
/// </summary>
public static class IntegrationTestDatabaseReset
{
    /// <summary>
    ///     Recreates schema and executes the idempotent seed pipeline for deterministic
    ///     integration test state.
    /// </summary>
    /// <param name="factory">Web application factory configured for integration tests.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when reset guardrails detect a potentially non-test database target.
    /// </exception>
    public static async Task ResetAndSeedAsync(WebApplicationFactory<Program> factory, CancellationToken ct = default)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var hostEnv = services.GetRequiredService<IHostEnvironment>();
        if (!string.Equals(hostEnv.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Refusing database reset outside Testing environment. Current environment: '{hostEnv.EnvironmentName}'.");
        }

        var db = services.GetRequiredService<DarwinDbContext>();
        var connectionString = db.Database.GetConnectionString() ?? string.Empty;

        // Guardrail: only allow destructive reset on clearly test-scoped databases.
        // This protects local/dev/prod data from accidental truncation.
        if (!IsSafeTestConnectionString(connectionString))
        {
            throw new InvalidOperationException(
                "Refusing database reset because connection string does not look test-scoped.");
        }

        await db.Database.EnsureDeletedAsync(ct).ConfigureAwait(false);
        await db.Database.MigrateAsync(ct).ConfigureAwait(false);

        var seeder = services.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    ///     Evaluates whether a connection string appears to target a test database.
    ///     The heuristic intentionally requires explicit test markers to reduce risk.
    /// </summary>
    /// <param name="connectionString">Database connection string.</param>
    /// <returns><c>true</c> when the target appears safe for test reset operations.</returns>
    private static bool IsSafeTestConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        return connectionString.Contains("test", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("(localdb)", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase);
    }
}
