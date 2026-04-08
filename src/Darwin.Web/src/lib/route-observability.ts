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

function toDegradedSurfaceKey(statusKey: string) {
  return statusKey.endsWith("Status")
    ? statusKey.slice(0, -1 * "Status".length)
    : statusKey;
}

function getDegradedSurfaceFootprint(degradedStatuses: Array<[string, unknown]>) {
  if (degradedStatuses.length === 0) {
    return undefined;
  }

  return degradedStatuses
    .map(([key, value]) => `${toDegradedSurfaceKey(key)}:${String(value)}`)
    .join("|");
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

function getAttentionLevel(input: {
  durationBand: string;
  degradedStatusCount?: number;
  failed?: boolean;
}) {
  if (input.failed) {
    return "high";
  }

  if (
    input.durationBand === "very-slow" ||
    (input.degradedStatusCount ?? 0) > 1
  ) {
    return "high";
  }

  if (
    input.durationBand === "slow" ||
    (input.degradedStatusCount ?? 0) === 1
  ) {
    return "medium";
  }

  return "low";
}

function getSignalKind(input: {
  degradedStatusCount?: number;
  failed?: boolean;
  isSlow?: boolean;
}) {
  if (input.failed) {
    return "failure";
  }

  if ((input.degradedStatusCount ?? 0) > 0 && input.isSlow) {
    return "performance-and-health";
  }

  if ((input.degradedStatusCount ?? 0) > 0) {
    return "health";
  }

  if (input.isSlow) {
    return "performance";
  }

  return "normal";
}

function getSuggestedAction(input: {
  degradedStatusCount?: number;
  failed?: boolean;
  isSlow?: boolean;
}) {
  if (input.failed) {
    return "inspect-failure-cause";
  }

  if ((input.degradedStatusCount ?? 0) > 0 && input.isSlow) {
    return "inspect-slow-and-degraded-dependencies";
  }

  if ((input.degradedStatusCount ?? 0) > 0) {
    return "inspect-degraded-dependencies";
  }

  if (input.isSlow) {
    return "inspect-slow-path";
  }

  return "none";
}

function buildObservedOutcomeDetail(
  durationMs: number,
  thresholdMs: number,
  degradedStatuses: Array<[string, unknown]>,
) {
  const degradedStatusCount = degradedStatuses.length;
  const degradedStatusMap =
    degradedStatusCount > 0 ? Object.fromEntries(degradedStatuses) : undefined;
  const degradedStatusKeys =
    degradedStatusCount > 0 ? degradedStatuses.map(([key]) => key) : undefined;
  const degradedSurfaceKeys =
    degradedStatusCount > 0
      ? degradedStatuses.map(([key]) => toDegradedSurfaceKey(key))
      : undefined;
  const isSlow = durationMs >= thresholdMs;
  const durationBand = getDurationBand(durationMs, thresholdMs);

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
    signalKind: getSignalKind({ degradedStatusCount, isSlow }),
    attentionLevel: getAttentionLevel({ durationBand, degradedStatusCount }),
    suggestedAction: getSuggestedAction({ degradedStatusCount, isSlow }),
    degradedStatusCount,
    degradedStatuses: degradedStatusMap,
    degradedStatusKeys,
    degradedSurfaceCount: degradedSurfaceKeys?.length ?? 0,
    degradedSurfaceKeys,
    degradedSurfaceFootprint: getDegradedSurfaceFootprint(degradedStatuses),
    primaryDegradedStatusKey: degradedStatusKeys?.[0],
    primaryDegradedSurface: degradedSurfaceKeys?.[0],
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
    const diagnosticDetail = {
      area: input.area,
      operation: input.operation,
      operationKey: `${input.area}:${input.operation}`,
      durationMs,
      ...outcomeDetail,
      ...(input.context ?? {}),
      ...(successDetail ?? {}),
    };

    if (durationMs >= thresholdMs) {
      warn("Darwin.Web slow operation", diagnosticDetail);
    } else if (degradedStatuses.length > 0 && shouldLogDegradedOperations()) {
      warn("Darwin.Web degraded operation", diagnosticDetail);
    }

    return result;
  } catch (cause) {
    const durationMs = now() - startedAt;
    const durationBand = getDurationBand(durationMs, thresholdMs);
    error("Darwin.Web failed operation", {
      area: input.area,
      operation: input.operation,
      operationKey: `${input.area}:${input.operation}`,
      durationMs,
      durationBand,
      healthState: "failed",
      outcomeKind: "failure",
      signalKind: "failure",
      attentionLevel: getAttentionLevel({ durationBand, failed: true }),
      suggestedAction: getSuggestedAction({ failed: true }),
      ...(input.context ?? {}),
      cause,
    });
    throw cause;
  }
}
