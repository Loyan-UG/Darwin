using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Darwin.Contracts.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Darwin.WebApi.Middleware
{
    /// <summary>
    /// Global exception handler that converts unhandled exceptions into
    /// RFC-7807-compatible problem responses using the shared ProblemDetails contract.
    /// Ensures consistent API error envelopes for mobile and external clients.
    /// </summary>
    public sealed class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the middleware.
        /// </summary>
        /// <param name="next">The next request delegate in the pipeline.</param>
        /// <param name="logger">Logger to record middleware events and failures.</param>
        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes the wrapped pipeline and catches unhandled exceptions.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while processing request {Path}", context.Request?.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// Maps exceptions into ProblemDetails responses.
        /// Safely handles cases where the response has already started.
        /// </summary>
        /// <param name="context">Current HTTP context.</param>
        /// <param name="ex">The exception that was thrown.</param>
        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            if (context.Response.HasStarted)
            {
                // Can't modify the response at this point; log and abort.
                _logger.LogWarning(ex, "Cannot write error response because the response has already started for request {Path}", context.Request?.Path);
                return;
            }

            var status = ex switch
            {
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                _ => (int)HttpStatusCode.InternalServerError
            };

            var title = status == (int)HttpStatusCode.Unauthorized
                ? "Unauthorized"
                : "API Error";

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = ex.Message,
                Instance = context.Request?.Path.Value
            };

            try
            {
                context.Response.Clear();
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json";

                var json = JsonSerializer.Serialize(problem);
                await context.Response.WriteAsync(json);
            }
            catch (Exception writeEx)
            {
                // If writing the problem response fails, log the failure.
                _logger.LogError(writeEx, "Failed to write error response for request {Path}", context.Request?.Path);
            }
        }
    }
}
