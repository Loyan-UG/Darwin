using Darwin.Application;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Application.HumanResources.Queries;
using Darwin.Application.HumanResources.Services;
using Darwin.Contracts.Common;
using Darwin.Contracts.HumanResources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Darwin.WebApi.Controllers.Member;

/// <summary>
/// Employee self-service payslip endpoints scoped to the authenticated member's linked employee record.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/member/payroll/payslips")]
public sealed class MemberPayrollPayslipsController : ApiControllerBase
{
    private readonly GetMyPayslipsPageHandler _getMyPayslipsPageHandler;
    private readonly DownloadMyPayslipDocumentHandler _downloadMyPayslipDocumentHandler;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    public MemberPayrollPayslipsController(
        GetMyPayslipsPageHandler getMyPayslipsPageHandler,
        DownloadMyPayslipDocumentHandler downloadMyPayslipDocumentHandler,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _getMyPayslipsPageHandler = getMyPayslipsPageHandler ?? throw new ArgumentNullException(nameof(getMyPayslipsPageHandler));
        _downloadMyPayslipDocumentHandler = downloadMyPayslipDocumentHandler ?? throw new ArgumentNullException(nameof(downloadMyPayslipDocumentHandler));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    /// <summary>
    /// Returns the current employee's payslip history.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<MemberPayslipSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMyPayslipsAsync([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct = default)
    {
        var normalizedPage = page.GetValueOrDefault(1);
        if (normalizedPage <= 0)
        {
            return BadRequestProblem(_validationLocalizer["PageMustBePositiveInteger"]);
        }

        var normalizedPageSize = pageSize.GetValueOrDefault(20);
        if (normalizedPageSize <= 0 || normalizedPageSize > 200)
        {
            return BadRequestProblem(_validationLocalizer["PageSizeMustBeBetween1And200"]);
        }

        var (items, total) = await _getMyPayslipsPageHandler
            .HandleAsync(normalizedPage, normalizedPageSize, ct)
            .ConfigureAwait(false);

        return Ok(new PagedResponse<MemberPayslipSummary>
        {
            Total = total,
            Items = items.Select(MapSummary).ToList(),
            Request = new PagedRequest
            {
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                Search = null
            }
        });
    }

    /// <summary>
    /// Downloads the official PDF payslip owned by the current employee.
    /// </summary>
    [HttpGet("{id:guid}/document")]
    [Produces(PayrollPayslipArtifactService.PdfContentType)]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocumentAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["IdentifierMustNotBeEmpty"]);
        }

        var result = await _downloadMyPayslipDocumentHandler.HandleAsync(id, ct).ConfigureAwait(false);
        if (!result.Succeeded || result.Value is null)
        {
            return NotFoundProblem(_validationLocalizer["PayrollPayslipNotFound"]);
        }

        return File(result.Value.Content, PayrollPayslipArtifactService.PdfContentType, result.Value.FileName);
    }

    private static MemberPayslipSummary MapSummary(MemberPayslipSummaryDto dto)
        => new()
        {
            Id = dto.Id,
            BusinessId = dto.BusinessId,
            BusinessName = dto.BusinessName,
            EmployeeId = dto.EmployeeId,
            EmployeeNumber = dto.EmployeeNumber,
            PayslipNumber = dto.PayslipNumber,
            Currency = dto.Currency,
            PeriodStartUtc = dto.PeriodStartUtc,
            PeriodEndUtc = dto.PeriodEndUtc,
            GrossPayMinor = dto.GrossPayMinor,
            EmployeeDeductionMinor = dto.EmployeeDeductionMinor,
            NetPayMinor = dto.NetPayMinor,
            Status = dto.Status,
            PaymentStatus = dto.PaymentStatus,
            GeneratedAtUtc = dto.GeneratedAtUtc,
            DocumentPath = dto.DocumentPath
        };
}
