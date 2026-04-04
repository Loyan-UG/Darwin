type ObserveAsyncOperationInput<T> = {
  area: string;
  operation: string;
  context?: Record<string, unknown>;
  getSuccessDetail?: (result: T) => Record<string, unknown> | undefined;
  thresholdMs?: number;
  now?: () => number;
  warn?: (message: string, detail: Record<string, unknown>) => void;
  error?: (message: string, detail: Record<string, unknown>) => void;
};

function shouldLogDegradedOperations() {
  return process.env.NODE_ENV === "production" ||
    process.env.DARWIN_WEB_LOG_DEGRADED === "true";
}

function getDegradedStatusEntries(detail?: Record<string, unknown>) {
  if (!detail) {
    return [];
  }

  return Object.entries(detail).filter(([key, value]) => {
    if (!key.endsWith("Status")) {
      return false;
    }

    return typeof value === "string" && value !== "ok";
  });
}

function getDurationBand(durationMs: number, thresholdMs: number) {
  if (durationMs < thresholdMs) {
    return "within-threshold";
  }

  if (durationMs >= thresholdMs * 3) {
    return "very-slow";
  }

  return "slow";
}

function buildObservedOutcomeDetail(
  durationMs: number,
  thresholdMs: number,
  degradedStatuses: Array<[string, unknown]>,
) {
  const durationBand = getDurationBand(durationMs, thresholdMs);
  const degradedStatusCount = degradedStatuses.length;
  const degradedStatusMap =
    degradedStatusCount > 0 ? Object.fromEntries(degradedStatuses) : undefined;
  const isSlow = durationMs >= thresholdMs;

  return {
    durationBand,
    healthState:
      degradedStatusCount > 1
        ? "multi-degraded"
        : degradedStatusCount === 1
          ? "degraded"
          : "healthy",
    outcomeKind:
      degradedStatusCount > 0
        ? isSlow
          ? "slow-degraded-success"
          : "degraded-success"
        : isSlow
          ? "slow-success"
          : "success",
    degradedStatusCount,
    degradedStatuses: degradedStatusMap,
  };
}

export async function observeAsyncOperation<T>(
  input: ObserveAsyncOperationInput<T>,
  work: () => Promise<T>,
) {
  const now = input.now ?? Date.now;
  const warn = input.warn ?? ((message, detail) => console.warn(message, detail));
  const error =
    input.error ?? ((message, detail) => console.error(message, detail));
  const thresholdMs = input.thresholdMs ?? 400;
  const startedAt = now();

  try {
    const result = await work();
    const durationMs = now() - startedAt;
    const successDetail = input.getSuccessDetail?.(result);
    const degradedStatuses = getDegradedStatusEntries(successDetail);
    const outcomeDetail = buildObservedOutcomeDetail(
      durationMs,
      thresholdMs,
      degradedStatuses,
    );

    if (durationMs >= thresholdMs) {
      warn("Darwin.Web slow operation", {
        area: input.area,
        operation: input.operation,
        durationMs,
        ...outcomeDetail,
        ...(input.context ?? {}),
        ...(successDetail ?? {}),
      });
    } else if (degradedStatuses.length > 0 && shouldLogDegradedOperations()) {
      warn("Darwin.Web degraded operation", {
        area: input.area,
        operation: input.operation,
        durationMs,
        ...outcomeDetail,
        ...(input.context ?? {}),
        ...(successDetail ?? {}),
      });
    }

    return result;
  } catch (cause) {
    const durationMs = now() - startedAt;
    error("Darwin.Web failed operation", {
      area: input.area,
      operation: input.operation,
      durationMs,
      durationBand: getDurationBand(durationMs, thresholdMs),
      healthState: "failed",
      outcomeKind: "failure",
      ...(input.context ?? {}),
      cause,
    });
    throw cause;
  }
}
