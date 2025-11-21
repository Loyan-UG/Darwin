using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Darwin.Contracts.Common;
using Microsoft.AspNetCore.Http;

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

        /// <summary>
        /// Initializes a new instance of the middleware.
        /// </summary>
        /// <param name="next">The next request delegate in the pipeline.</param>
        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
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
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// Maps exceptions into ProblemDetails responses.
        /// </summary>
        /// <param name="context">Current HTTP context.</param>
        /// <param name="ex">The exception that was thrown.</param>
        private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
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

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";

            var json = JsonSerializer.Serialize(problem);
            await context.Response.WriteAsync(json);
        }
    }
}
