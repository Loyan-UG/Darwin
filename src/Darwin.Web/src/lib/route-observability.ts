type ObserveAsyncOperationInput = {
  area: string;
  operation: string;
  thresholdMs?: number;
  now?: () => number;
  warn?: (message: string, detail: Record<string, unknown>) => void;
  error?: (message: string, detail: Record<string, unknown>) => void;
};

export async function observeAsyncOperation<T>(
  input: ObserveAsyncOperationInput,
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

    if (durationMs >= thresholdMs) {
      warn("Darwin.Web slow operation", {
        area: input.area,
        operation: input.operation,
        durationMs,
      });
    }

    return result;
  } catch (cause) {
    const durationMs = now() - startedAt;
    error("Darwin.Web failed operation", {
      area: input.area,
      operation: input.operation,
      durationMs,
      cause,
    });
    throw cause;
  }
}
