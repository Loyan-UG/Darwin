import { cache } from "react";
import { observeAsyncOperation } from "@/lib/route-observability";

type ObservedLoaderConfig<Args extends unknown[], Result> = {
  area: string;
  operation: string;
  thresholdMs?: number;
  normalizeArgs?: (...args: Args) => Args;
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
  return (...rawArgs: Args) => {
    const args = config.normalizeArgs?.(...rawArgs) ?? rawArgs;

    return observeAsyncOperation(
      {
        area: config.area,
        operation: config.operation,
        context: config.getContext?.(...args),
        getSuccessDetail: (result) => config.getSuccessContext?.(result, ...args),
        thresholdMs: config.thresholdMs,
      },
      () => config.load(...args),
    );
  };
}

export function createCachedObservedLoader<Args extends unknown[], Result>(
  config: ObservedLoaderConfig<Args, Result>,
) {
  const load = createObservedLoader(config);
  const cached = cache((...args: Args) => load(...args));

  return (...rawArgs: Args) => {
    const args = config.normalizeArgs?.(...rawArgs) ?? rawArgs;
    return cached(...args);
  };
}
