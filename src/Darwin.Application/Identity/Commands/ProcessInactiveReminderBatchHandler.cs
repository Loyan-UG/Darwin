using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Shared.Results;

namespace Darwin.Application.Identity.Commands;

/// <summary>
/// Orchestrates one inactive-reminder batch:
/// select candidates, dispatch reminders, and persist per-outcome measurement metadata.
/// </summary>
public sealed class ProcessInactiveReminderBatchHandler
{
    private readonly GetInactiveReminderCandidatesHandler _getCandidatesHandler;
    private readonly MarkInactiveReminderAttemptHandler _markAttemptHandler;
    private readonly IInactiveReminderDispatcher _dispatcher;

    public ProcessInactiveReminderBatchHandler(
        GetInactiveReminderCandidatesHandler getCandidatesHandler,
        MarkInactiveReminderAttemptHandler markAttemptHandler,
        IInactiveReminderDispatcher dispatcher)
    {
        _getCandidatesHandler = getCandidatesHandler ?? throw new ArgumentNullException(nameof(getCandidatesHandler));
        _markAttemptHandler = markAttemptHandler ?? throw new ArgumentNullException(nameof(markAttemptHandler));
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
            MaxItems = request.MaxItems,
            IncludeSuppressedByCooldown = true
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

            if (candidate.IsSuppressed)
            {
                var suppressionCode = candidate.SuppressionCode ?? "CooldownActive";
                summary.SuppressedCount++;
                summary.SuppressedByCooldownCount++;
                IncrementCount(summary.SuppressionCodeCounts, suppressionCode);
                await _markAttemptHandler.HandleAsync(new MarkInactiveReminderAttemptDto
                {
                    UserId = candidate.UserId,
                    Outcome = "Suppressed",
                    OutcomeCode = suppressionCode
                }, ct).ConfigureAwait(false);

                continue;
            }

            if (string.IsNullOrWhiteSpace(candidate.PushDestinationDeviceId)
                || string.IsNullOrWhiteSpace(candidate.PushToken))
            {
                const string missingDestinationCode = "NoPushDestination";
                summary.SuppressedCount++;
                summary.SuppressedByMissingDestinationCount++;
                IncrementCount(summary.SuppressionCodeCounts, missingDestinationCode);
                await _markAttemptHandler.HandleAsync(new MarkInactiveReminderAttemptDto
                {
                    UserId = candidate.UserId,
                    Outcome = "Suppressed",
                    OutcomeCode = missingDestinationCode
                }, ct).ConfigureAwait(false);

                continue;
            }

            var dispatchResult = await _dispatcher
                .DispatchAsync(candidate.UserId, candidate.PushDestinationDeviceId, candidate.PushToken, candidate.Platform, candidate.InactiveDays, ct)
                .ConfigureAwait(false);

            if (!dispatchResult.Succeeded)
            {
                var failureCode = string.IsNullOrWhiteSpace(dispatchResult.Error)
                    ? "Gateway.UnknownFailure"
                    : dispatchResult.Error;
                summary.FailedCount++;
                IncrementCount(summary.FailureCodeCounts, failureCode);
                await _markAttemptHandler.HandleAsync(new MarkInactiveReminderAttemptDto
                {
                    UserId = candidate.UserId,
                    Outcome = "Failed",
                    OutcomeCode = failureCode
                }, ct).ConfigureAwait(false);

                continue;
            }

            var markResult = await _markAttemptHandler
                .HandleAsync(new MarkInactiveReminderAttemptDto
                {
                    UserId = candidate.UserId,
                    Outcome = "Sent"
                }, ct)
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

    /// <summary>
    /// Increments one aggregate observability counter in a case-insensitive dictionary.
    /// </summary>
    private static void IncrementCount(IDictionary<string, int> counters, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        counters[code] = counters.TryGetValue(code, out var current)
            ? current + 1
            : 1;
    }
}
