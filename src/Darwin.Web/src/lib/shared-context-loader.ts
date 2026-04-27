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

export function buildSharedContextLoaderObservationContext(
  kind: SharedContextKind,
  context?: Record<string, unknown>,
  options?: {
    hasCanonicalNormalization?: boolean;
  },
) {
  return buildSharedContextBaseDiagnostics(kind, {
    hasCanonicalNormalization: options?.hasCanonicalNormalization,
    extras: context,
  });
}

export function buildSharedContextLoaderSuccessContext(
  kind: SharedContextKind,
  context?: Record<string, unknown>,
  options?: {
    hasCanonicalNormalization?: boolean;
  },
) {
  return buildSharedContextBaseDiagnostics(kind, {
    hasCanonicalNormalization: options?.hasCanonicalNormalization,
    extras: context,
  });
}

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
      buildSharedContextLoaderObservationContext(kind, getContext(...args) ?? {}, {
        hasCanonicalNormalization: Boolean(normalizeArgs),
      }),
    getSuccessContext: (result: TResult, ...args: TArgs) =>
      buildSharedContextLoaderSuccessContext(
        kind,
        getSuccessContext(result, ...args) ?? {},
        {
          hasCanonicalNormalization: Boolean(normalizeArgs),
        },
      ),
    load,
  });
}
