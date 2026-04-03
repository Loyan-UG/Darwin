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

    if (durationMs >= thresholdMs) {
      warn("Darwin.Web slow operation", {
        area: input.area,
        operation: input.operation,
        durationMs,
        ...(input.context ?? {}),
        ...(successDetail ?? {}),
      });
    } else if (degradedStatuses.length > 0) {
      warn("Darwin.Web degraded operation", {
        area: input.area,
        operation: input.operation,
        durationMs,
        ...(input.context ?? {}),
        ...(successDetail ?? {}),
        degradedStatusCount: degradedStatuses.length,
        degradedStatuses: Object.fromEntries(degradedStatuses),
      });
    }

    return result;
  } catch (cause) {
    const durationMs = now() - startedAt;
    error("Darwin.Web failed operation", {
      area: input.area,
      operation: input.operation,
      durationMs,
      ...(input.context ?? {}),
      cause,
    });
    throw cause;
  }
}
