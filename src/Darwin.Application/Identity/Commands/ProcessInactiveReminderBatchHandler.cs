using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Shared.Results;

namespace Darwin.Application.Identity.Commands;

/// <summary>
/// Orchestrates one inactive-reminder batch:
/// select candidates, dispatch reminder, and mark successful sends.
/// </summary>
public sealed class ProcessInactiveReminderBatchHandler
{
    private readonly GetInactiveReminderCandidatesHandler _getCandidatesHandler;
    private readonly MarkInactiveReminderSentHandler _markSentHandler;
    private readonly IInactiveReminderDispatcher _dispatcher;

    public ProcessInactiveReminderBatchHandler(
        GetInactiveReminderCandidatesHandler getCandidatesHandler,
        MarkInactiveReminderSentHandler markSentHandler,
        IInactiveReminderDispatcher dispatcher)
    {
        _getCandidatesHandler = getCandidatesHandler ?? throw new ArgumentNullException(nameof(getCandidatesHandler));
        _markSentHandler = markSentHandler ?? throw new ArgumentNullException(nameof(markSentHandler));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <summary>
    /// Executes one batch and returns aggregate counters for observability.
    /// </summary>
    public async Task<Result<ProcessInactiveReminderBatchResultDto>> HandleAsync(
        ProcessInactiveReminderBatchDto request,
        CancellationToken ct = default)
    {
        if (request is null)
        {
            return Result<ProcessInactiveReminderBatchResultDto>.Fail("Request payload is required.");
        }

        var candidatesResult = await _getCandidatesHandler.HandleAsync(new GetInactiveReminderCandidatesDto
        {
            InactiveThresholdDays = request.InactiveThresholdDays,
            CooldownHours = request.CooldownHours,
            MaxItems = request.MaxItems
        }, ct).ConfigureAwait(false);

        if (!candidatesResult.Succeeded || candidatesResult.Value is null)
        {
            return Result<ProcessInactiveReminderBatchResultDto>.Fail(candidatesResult.Error ?? "Could not resolve inactive reminder candidates.");
        }

        var summary = new ProcessInactiveReminderBatchResultDto();

        foreach (var candidate in candidatesResult.Value)
        {
            ct.ThrowIfCancellationRequested();
            summary.CandidatesEvaluated++;

            if (string.IsNullOrWhiteSpace(candidate.PushDestinationDeviceId))
            {
                summary.SuppressedCount++;
                continue;
            }

            var dispatchResult = await _dispatcher
                .DispatchAsync(candidate.UserId, candidate.PushDestinationDeviceId, candidate.InactiveDays, ct)
                .ConfigureAwait(false);

            if (!dispatchResult.Succeeded)
            {
                summary.FailedCount++;
                continue;
            }

            var markResult = await _markSentHandler
                .HandleAsync(new MarkInactiveReminderSentDto { UserId = candidate.UserId }, ct)
                .ConfigureAwait(false);

            if (!markResult.Succeeded)
            {
                summary.FailedCount++;
                continue;
            }

            summary.DispatchedCount++;
        }

        return Result<ProcessInactiveReminderBatchResultDto>.Ok(summary);
    }
}
