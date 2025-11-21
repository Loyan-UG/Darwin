using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Services;
using Darwin.Contracts.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Darwin.WebApi.Controllers
{
    /// <summary>
    /// Provides lightweight meta endpoints for health checks and
    /// environment/bootstrapping information used by mobile clients and monitoring.
    /// </summary>
    [ApiController]
    [Route("api/meta")]
    public sealed class MetaController : ControllerBase
    {
        private static readonly DateTime _processStartUtc = DateTime.UtcNow;

        private readonly IHostEnvironment _hostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IClock _clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetaController"/> class.
        /// </summary>
        /// <param name="hostEnvironment">
        /// The ASP.NET Core host environment abstraction providing environment name and application name.
        /// </param>
        /// <param name="configuration">
        /// The configuration root used to read basic meta information such as logical ApplicationName.
        /// </param>
        /// <param name="clock">
        /// Application-level clock abstraction used to obtain the current UTC time in a testable way.
        /// </param>
        public MetaController(
            IHostEnvironment hostEnvironment,
            IConfiguration configuration,
            IClock clock)
        {
            _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Lightweight health endpoint that can be used by load balancers and uptime monitors
        /// to verify that the WebApi process is alive and able to handle requests.
        /// </summary>
        /// <remarks>
        /// This endpoint deliberately performs no I/O and does not touch the database.
        /// For deeper health probes, consider adding a separate endpoint that checks persistence and external services.
        /// </remarks>
        /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A standard API envelope with basic health information such as current UTC time and uptime.
        /// </returns>
        [HttpGet("health")]
        [ProducesResponseType(typeof(ApiEnvelope<object>), 200)]
        public ActionResult<ApiEnvelope<object>> GetHealth(CancellationToken ct)
        {
            // NOTE: No blocking work is performed here. The token is accepted for future extensibility.
            var nowUtc = _clock.UtcNow;

            var payload = new
            {
                status = "Healthy",
                serverTimeUtc = nowUtc,
                uptimeSeconds = (nowUtc - _processStartUtc).TotalSeconds,
                environment = _hostEnvironment.EnvironmentName,
                application = ResolveApplicationName()
            };

            return Ok(ApiEnvelope<object>.Ok(payload));
        }

        /// <summary>
        /// Returns basic application meta information that can be used by mobile apps
        /// for diagnostics screens or to display backend version/build data.
        /// </summary>
        /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A standard API envelope containing assembly version, build information, environment and uptime.
        /// </returns>
        [HttpGet("info")]
        [ProducesResponseType(typeof(ApiEnvelope<object>), 200)]
        public ActionResult<ApiEnvelope<object>> GetInfo(CancellationToken ct)
        {
            var nowUtc = _clock.UtcNow;
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "unknown";

            // Optional additional build metadata that may be provided through configuration or CI variables.
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
                startedAtUtc = _processStartUtc,
                uptimeSeconds = (nowUtc - _processStartUtc).TotalSeconds
            };

            return Ok(ApiEnvelope<object>.Ok(payload));
        }

        /// <summary>
        /// Resolves a logical application name to use in responses, falling back to the host environment
        /// application name and assembly name if explicit configuration is not present.
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
