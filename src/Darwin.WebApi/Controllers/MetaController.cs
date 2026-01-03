using Darwin.Application.Abstractions.Services;
using Darwin.Application.Meta.Queries;
using Darwin.Contracts.Meta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.WebApi.Controllers
{
    /// <summary>
    /// Provides meta endpoints for health checks and basic diagnostic information.
    /// These endpoints are used by mobile clients and monitoring systems to
    /// verify that the Web API is running and to inspect environment/version data.
    /// </summary>
    [ApiController]
    [Route("api/v1/meta")]
    public sealed class MetaController : ApiControllerBase
    {
        private static readonly DateTime ProcessStartUtc = DateTime.UtcNow;

        private readonly IHostEnvironment _hostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IClock _clock;
        private readonly ILogger<MetaController> _logger;
        private readonly GetAppBootstrapHandler _getAppBootstrap;


        /// <summary>
        /// Initializes a new instance of the <see cref="MetaController"/>.
        /// </summary>
        /// <param name="clock">Clock abstraction used for deterministic time reads.</param>
        /// <param name="getAppBootstrap">Application handler that returns mobile bootstrap configuration.</param>
        /// <param name="configuration">Application configuration source.</param>
        /// <param name="hostEnvironment">Host environment information (dev/staging/prod).</param>
        /// <param name="logger">Logger used for diagnostics.</param>
        public MetaController(
            IClock clock,
            GetAppBootstrapHandler getAppBootstrap,
            IConfiguration configuration,
            IHostEnvironment hostEnvironment,
            ILogger<MetaController> logger)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _getAppBootstrap = getAppBootstrap ?? throw new ArgumentNullException(nameof(getAppBootstrap));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Lightweight health endpoint used by load balancers and uptime monitors
        /// to verify that the Web API process is alive and able to handle requests.
        /// </summary>
        /// <remarks>
        /// This endpoint deliberately performs no I/O and does not touch the database.
        /// For deeper health probes, a separate endpoint that checks persistence and
        /// external services can be added later.
        /// </remarks>
        /// <param name="ct">The cancellation token for the request.</param>
        /// <returns>A JSON payload describing the current health state.</returns>
        [HttpGet("health")]
        [ProducesResponseType(typeof(object), 200)]
        public ActionResult GetHealth(CancellationToken ct)
        {
            // Cancellation token is accepted for future extensibility, but no
            // blocking work is performed here.
            var nowUtc = _clock.UtcNow;
            var uptime = nowUtc - ProcessStartUtc;

            var payload = new
            {
                status = "Healthy",
                serverTimeUtc = nowUtc,
                uptimeSeconds = uptime.TotalSeconds,
                environment = _hostEnvironment.EnvironmentName,
                application = ResolveApplicationName()
            };

            _logger.LogDebug(
                "Health check requested. Status={Status}, UptimeSeconds={UptimeSeconds}, Environment={Environment}",
                payload.status,
                payload.uptimeSeconds,
                payload.environment);

            return Ok(payload);
        }

        /// <summary>
        /// Returns meta information such as application name, environment, version,
        /// build metadata and current uptime. This is typically used by mobile apps
        /// for diagnostics screens and by operators during troubleshooting.
        /// </summary>
        /// <param name="ct">The cancellation token for the request.</param>
        /// <returns>A JSON payload describing application meta information.</returns>
        [HttpGet("info")]
        [ProducesResponseType(typeof(object), 200)]
        public ActionResult GetInfo(CancellationToken ct)
        {
            var nowUtc = _clock.UtcNow;
            var uptime = nowUtc - ProcessStartUtc;

            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "unknown";

            // Optional additional build metadata, typically supplied by CI/CD.
            var buildNumber = _configuration["Build:Number"];
            var commitHash = _configuration["Build:Commit"];

            var payload = new
            {
                application = ResolveApplicationName(),
                environment = _hostEnvironment.EnvironmentName,
                version,
                buildNumber,
                commitHash,
                serverTimeUtc = nowUtc,
                startedAtUtc = ProcessStartUtc,
                uptimeSeconds = uptime.TotalSeconds
            };

            _logger.LogInformation(
                "Meta info requested. Application={Application}, Environment={Environment}, Version={Version}, Build={BuildNumber}, Commit={CommitHash}",
                payload.application,
                payload.environment,
                payload.version,
                payload.buildNumber,
                payload.commitHash);

            return Ok(payload);
        }

        /// <summary>
        /// Resolves a logical application name to use in responses.
        /// Prefers configuration, then host environment, then the entry assembly name.
        /// </summary>
        /// <returns>The resolved application name.</returns>
        private string ResolveApplicationName()
        {
            // Prefer an explicit logical name from configuration if provided.
            var configuredName = _configuration["Application:Name"];
            if (!string.IsNullOrWhiteSpace(configuredName))
            {
                return configuredName!;
            }

            // Fallback to host environment application name.
            if (!string.IsNullOrWhiteSpace(_hostEnvironment.ApplicationName))
            {
                return _hostEnvironment.ApplicationName;
            }

            // As a last resort, use the entry assembly name.
            var entryName = Assembly.GetEntryAssembly()?.GetName().Name;
            return string.IsNullOrWhiteSpace(entryName) ? "Darwin.WebApi" : entryName!;
        }



        /// <summary>
        /// Returns minimal non-sensitive bootstrap configuration for mobile apps.
        /// This endpoint must be thin glue and delegate the source of truth to Application.
        /// </summary>
        [HttpGet("bootstrap")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AppBootstrapResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AppBootstrapResponse>> GetBootstrapAsync(CancellationToken ct)
        {
            var result = await _getAppBootstrap.HandleAsync(ct).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                // Keep response intentionally small and consistent.
                return BadRequest(new { error = result.Error ?? "Bootstrap request failed." });
            }

            if (result.Value is null)
            {
                // Defensive: success with null payload should not happen, but we must be null-safe.
                return BadRequest(new { error = "Bootstrap payload is empty. This is a server bug." });
            }

            var dto = result.Value;

            var response = new AppBootstrapResponse
            {
                JwtAudience = dto.JwtAudience,
                QrTokenRefreshSeconds = dto.QrTokenRefreshSeconds,
                MaxOutboxItems = dto.MaxOutboxItems
            };

            return Ok(response);
        }

    }
}
