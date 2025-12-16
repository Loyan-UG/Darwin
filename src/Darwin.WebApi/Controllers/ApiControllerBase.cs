using System;
using Darwin.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// There is also Microsoft.AspNetCore.Mvc.ProblemDetails.
using ContractProblemDetails = Darwin.Contracts.Common.ProblemDetails;

namespace Darwin.WebApi.Controllers
{
    /// <summary>
    /// Base class for WebApi controllers to centralize consistent HTTP error shaping.
    /// </summary>
    /// <remarks>
    /// This API uses the shared contract type <see cref="ContractProblemDetails"/> for error payloads.
    /// We intentionally do not return <c>Microsoft.AspNetCore.Mvc.ProblemDetails</c> to keep
    /// server and mobile clients aligned via Darwin.Contracts.
    /// </remarks>
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>
        /// Creates a consistent 400 (Bad Request) response using the shared contracts problem shape.
        /// </summary>
        /// <param name="title">Short summary of the problem.</param>
        /// <param name="detail">Optional detail message.</param>
        /// <returns>HTTP 400 response with <see cref="ContractProblemDetails"/> body.</returns>
        protected IActionResult BadRequestProblem(string title, string? detail = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title is required.", nameof(title));

            var problem = new ContractProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = title.Trim(),
                Detail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim(),
                Instance = HttpContext?.Request?.Path.Value
            };

            return StatusCode(StatusCodes.Status400BadRequest, problem);
        }

        /// <summary>
        /// Creates a consistent 404 (Not Found) response using the shared contracts problem shape.
        /// </summary>
        /// <param name="title">Short summary of the problem.</param>
        /// <param name="detail">Optional detail message.</param>
        /// <returns>HTTP 404 response with <see cref="ContractProblemDetails"/> body.</returns>
        protected IActionResult NotFoundProblem(string title, string? detail = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title is required.", nameof(title));

            var problem = new ContractProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = title.Trim(),
                Detail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim(),
                Instance = HttpContext?.Request?.Path.Value
            };

            return StatusCode(StatusCodes.Status404NotFound, problem);
        }

        /// <summary>
        /// Converts a failed non-generic <see cref="Result"/> into a standardized 400 response.
        /// </summary>
        /// <param name="result">Operation result.</param>
        /// <param name="fallbackTitle">Fallback message when <see cref="Result.Error"/> is null/empty.</param>
        /// <returns>HTTP 400 response with <see cref="ContractProblemDetails"/> body.</returns>
        protected IActionResult ProblemFromResult(Result result, string fallbackTitle = "Request failed.")
        {
            if (result is null) throw new ArgumentNullException(nameof(result));

            if (result.Succeeded)
            {
                // This method must never be used for successful results.
                // Treat this as a bug rather than silently returning 200.
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            var title = string.IsNullOrWhiteSpace(result.Error) ? fallbackTitle : result.Error!;

            var problem = new ContractProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = title,
                Detail = null,
                Instance = HttpContext?.Request?.Path.Value
            };

            return StatusCode(StatusCodes.Status400BadRequest, problem);
        }

        /// <summary>
        /// Converts a failed generic <see cref="Result{T}"/> into a standardized 400 response.
        /// </summary>
        /// <typeparam name="T">Payload type.</typeparam>
        /// <param name="result">Operation result.</param>
        /// <param name="fallbackTitle">Fallback message when <see cref="Result{T}.Error"/> is null/empty.</param>
        /// <returns>HTTP 400 response with <see cref="ContractProblemDetails"/> body.</returns>
        protected IActionResult ProblemFromResult<T>(Result<T> result, string fallbackTitle = "Request failed.")
        {
            if (result is null) throw new ArgumentNullException(nameof(result));

            if (result.Succeeded)
            {
                // This method must never be used for successful results.
                // Treat this as a bug rather than silently returning 200.
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            var title = string.IsNullOrWhiteSpace(result.Error) ? fallbackTitle : result.Error!;

            var problem = new ContractProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = title,
                Detail = null,
                Instance = HttpContext?.Request?.Path.Value
            };

            return StatusCode(StatusCodes.Status400BadRequest, problem);
        }
    }
}
