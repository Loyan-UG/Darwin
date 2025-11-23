using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Darwin.WebApi.Controllers
{
    /// <summary>
    /// Provides meta endpoints for health checks and basic diagnostic information.
    /// These endpoints are used by mobile clients and monitoring systems to
    /// verify that the Web API is running and to inspect environment/version data.
    /// </summary>
    [ApiController]
    [Route("api/meta")]
    public sealed class MetaController : ControllerBase
    {
        private static readonly DateTime ProcessStartUtc = DateTime.UtcNow;

        private readonly IHostEnvironment _hostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IClock _clock;
        private readonly ILogger<MetaController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetaController"/> class.
        /// </summary>
        /// <param name="hostEnvironment">The ASP.NET Core host environment abstraction.</param>
        /// <param name="configuration">The configuration root for reading meta settings.</param>
        /// <param name="clock">The application-level clock abstraction.</param>
        /// <param name="logger">The logger used to emit diagnostic log entries.</param>
        public MetaController(
            IHostEnvironment hostEnvironment,
            IConfiguration configuration,
            IClock clock,
            ILogger<MetaController> logger)
        {
            _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
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
    }
}
