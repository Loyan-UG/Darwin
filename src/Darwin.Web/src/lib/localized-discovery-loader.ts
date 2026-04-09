import { createCachedObservedLoader } from "@/lib/observed-loader";
import {
  buildLocalizedDiscoveryLoaderBaseDiagnostics,
  buildLocalizedDiscoveryLoaderSuccessDiagnostics,
} from "@/lib/localized-discovery-loader-diagnostics";

type LocalizedDiscoveryLoaderKind = "inventory" | "sitemap";

type CreateLocalizedDiscoveryLoaderOptions<TResult> = {
  kind: LocalizedDiscoveryLoaderKind;
  area: string;
  operation: string;
  thresholdMs?: number;
  getContext: () => Record<string, unknown> | undefined;
  getSuccessContext: (result: TResult) => Record<string, unknown> | undefined;
  load: () => Promise<TResult>;
};

export function buildLocalizedDiscoveryLoaderDiagnostics(
  kind: LocalizedDiscoveryLoaderKind,
  extras?: Record<string, unknown>,
) {
  return buildLocalizedDiscoveryLoaderBaseDiagnostics(kind, {
    hasCanonicalCultureNormalization: true,
    extras,
  });
}

export function createLocalizedDiscoveryLoader<TResult>({
  kind,
  area,
  operation,
  thresholdMs = 325,
  getContext,
  getSuccessContext,
  load,
}: CreateLocalizedDiscoveryLoaderOptions<TResult>) {
  return createCachedObservedLoader({
    area,
    operation,
    thresholdMs,
    getContext: () =>
      buildLocalizedDiscoveryLoaderBaseDiagnostics(kind, {
        hasCanonicalCultureNormalization: true,
        extras: getContext(),
      }),
    getSuccessContext: (result: TResult) =>
      buildLocalizedDiscoveryLoaderSuccessDiagnostics(
        kind,
        getSuccessContext(result),
        {
          hasCanonicalCultureNormalization: true,
        },
      ),
    load,
  });
}
