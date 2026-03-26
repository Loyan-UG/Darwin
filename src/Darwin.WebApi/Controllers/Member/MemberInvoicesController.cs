using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Queries;
using Darwin.Contracts.Common;
using Darwin.Contracts.Invoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebApi.Controllers.Member;

/// <summary>
/// Member-scoped invoice history endpoints for the front-office member portal and consumer apps.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/member/invoices")]
public sealed class MemberInvoicesController : ApiControllerBase
{
    private readonly GetMyInvoicesPageHandler _getMyInvoicesPageHandler;
    private readonly GetMyInvoiceDetailHandler _getMyInvoiceDetailHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberInvoicesController"/> class.
    /// </summary>
    public MemberInvoicesController(
        GetMyInvoicesPageHandler getMyInvoicesPageHandler,
        GetMyInvoiceDetailHandler getMyInvoiceDetailHandler)
    {
        _getMyInvoicesPageHandler = getMyInvoicesPageHandler ?? throw new ArgumentNullException(nameof(getMyInvoicesPageHandler));
        _getMyInvoiceDetailHandler = getMyInvoiceDetailHandler ?? throw new ArgumentNullException(nameof(getMyInvoiceDetailHandler));
    }

    /// <summary>
    /// Returns a paged invoice history for the current authenticated member.
    /// </summary>
    [HttpGet]
    [HttpGet("/api/v1/invoices")]
    [ProducesResponseType(typeof(PagedResponse<MemberInvoiceSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMyInvoicesAsync([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct = default)
    {
        var normalizedPage = page.GetValueOrDefault(1);
        if (normalizedPage <= 0)
        {
            return BadRequestProblem("Page must be a positive integer.");
        }

        var normalizedPageSize = pageSize.GetValueOrDefault(20);
        if (normalizedPageSize <= 0 || normalizedPageSize > 200)
        {
            return BadRequestProblem("PageSize must be between 1 and 200.");
        }

        var (items, total) = await _getMyInvoicesPageHandler
            .HandleAsync(normalizedPage, normalizedPageSize, ct)
            .ConfigureAwait(false);

        return Ok(new PagedResponse<MemberInvoiceSummary>
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
    /// Returns the detail of a single invoice owned by the current authenticated member.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HttpGet("/api/v1/invoices/{id:guid}")]
    [ProducesResponseType(typeof(MemberInvoiceDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyInvoiceAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequestProblem("Id must not be empty.");
        }

        var dto = await _getMyInvoiceDetailHandler.HandleAsync(id, ct).ConfigureAwait(false);
        if (dto is null)
        {
            return NotFoundProblem("Invoice not found.");
        }

        return Ok(MapDetail(dto));
    }

    private static MemberInvoiceSummary MapSummary(MemberInvoiceSummaryDto dto)
        => new()
        {
            Id = dto.Id,
            BusinessId = dto.BusinessId,
            BusinessName = dto.BusinessName,
            OrderId = dto.OrderId,
            OrderNumber = dto.OrderNumber,
            Currency = dto.Currency,
            TotalGrossMinor = dto.TotalGrossMinor,
            RefundedAmountMinor = dto.RefundedAmountMinor,
            SettledAmountMinor = dto.SettledAmountMinor,
            BalanceMinor = dto.BalanceMinor,
            Status = dto.Status.ToString(),
            DueDateUtc = dto.DueDateUtc,
            PaidAtUtc = dto.PaidAtUtc,
            CreatedAtUtc = dto.CreatedAtUtc
        };

    private static MemberInvoiceDetail MapDetail(MemberInvoiceDetailDto dto)
        => new()
        {
            Id = dto.Id,
            BusinessId = dto.BusinessId,
            BusinessName = dto.BusinessName,
            OrderId = dto.OrderId,
            OrderNumber = dto.OrderNumber,
            Currency = dto.Currency,
            TotalGrossMinor = dto.TotalGrossMinor,
            RefundedAmountMinor = dto.RefundedAmountMinor,
            SettledAmountMinor = dto.SettledAmountMinor,
            BalanceMinor = dto.BalanceMinor,
            Status = dto.Status.ToString(),
            DueDateUtc = dto.DueDateUtc,
            PaidAtUtc = dto.PaidAtUtc,
            CreatedAtUtc = dto.CreatedAtUtc,
            TotalNetMinor = dto.TotalNetMinor,
            TotalTaxMinor = dto.TotalTaxMinor,
            PaymentSummary = dto.PaymentSummary
        };
}
