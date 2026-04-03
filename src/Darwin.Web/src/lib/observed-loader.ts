import { cache } from "react";
import { observeAsyncOperation } from "@/lib/route-observability";

type ObservedLoaderConfig<Args extends unknown[], Result> = {
  area: string;
  operation: string;
  thresholdMs?: number;
  getContext?: (...args: Args) => Record<string, unknown> | undefined;
  getSuccessContext?: (
    result: Result,
    ...args: Args
  ) => Record<string, unknown> | undefined;
  load: (...args: Args) => Promise<Result>;
};

export function createObservedLoader<Args extends unknown[], Result>(
  config: ObservedLoaderConfig<Args, Result>,
) {
  return (...args: Args) =>
    observeAsyncOperation(
      {
        area: config.area,
        operation: config.operation,
        context: config.getContext?.(...args),
        getSuccessDetail: (result) => config.getSuccessContext?.(result, ...args),
        thresholdMs: config.thresholdMs,
      },
      () => config.load(...args),
    );
}

export function createCachedObservedLoader<Args extends unknown[], Result>(
  config: ObservedLoaderConfig<Args, Result>,
) {
  const load = createObservedLoader(config);
  const cached = cache((...args: Args) => load(...args));

  return (...args: Args) => cached(...args);
}
