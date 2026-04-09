import { createCachedObservedLoader } from "@/lib/observed-loader";
import {
  buildSharedContextBaseDiagnostics,
  type SharedContextKind,
} from "@/lib/shared-context-diagnostics";

type CreateSharedContextLoaderOptions<TArgs extends unknown[], TResult> = {
  kind: SharedContextKind;
  area: string;
  operation: string;
  thresholdMs?: number;
  normalizeArgs?: (...args: TArgs) => TArgs;
  getContext: (...args: TArgs) => Record<string, unknown> | undefined;
  getSuccessContext: (
    result: TResult,
    ...args: TArgs
  ) => Record<string, unknown> | undefined;
  load: (...args: TArgs) => Promise<TResult>;
};

export function createSharedContextLoader<TArgs extends unknown[], TResult>({
  kind,
  area,
  operation,
  thresholdMs = 250,
  normalizeArgs,
  getContext,
  getSuccessContext,
  load,
}: CreateSharedContextLoaderOptions<TArgs, TResult>) {
  return createCachedObservedLoader({
    area,
    operation,
    thresholdMs,
    normalizeArgs,
    getContext: (...args: TArgs) =>
      buildSharedContextBaseDiagnostics(kind, {
        hasCanonicalNormalization: Boolean(normalizeArgs),
        extras: getContext(...args) ?? {},
      }),
    getSuccessContext: (result: TResult, ...args: TArgs) =>
      buildSharedContextBaseDiagnostics(kind, {
        hasCanonicalNormalization: Boolean(normalizeArgs),
        extras: getSuccessContext(result, ...args) ?? {},
      }),
    load,
  });
}
