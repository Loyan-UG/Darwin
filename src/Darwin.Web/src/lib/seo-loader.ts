import type { Metadata } from "next";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeSeoMetadataHealth } from "@/lib/route-health";
import {
  buildSeoLoaderBaseDiagnostics,
  buildSeoSuccessDiagnostics,
} from "@/lib/seo-loader-diagnostics";

export type SeoMetadataPayload = {
  metadata: Metadata;
  canonicalPath: string;
  noIndex: boolean;
  languageAlternates?: Record<string, string>;
};

export function buildSeoLoaderObservationContext(
  area: string,
  context?: Record<string, unknown>,
  options?: {
    hasCanonicalNormalization?: boolean;
  },
) {
  return buildSeoLoaderBaseDiagnostics(area, {
    hasCanonicalNormalization: options?.hasCanonicalNormalization,
    extras: context,
  });
}

export function buildSeoLoaderSuccessContext(
  area: string,
  result: SeoMetadataPayload,
  options?: {
    hasCanonicalNormalization?: boolean;
  },
) {
  return {
    ...buildSeoSuccessDiagnostics(area, result, {
      hasCanonicalNormalization: options?.hasCanonicalNormalization,
    }),
    ...summarizeSeoMetadataHealth(result),
  };
}

type CreateSeoLoaderOptions<TArgs extends unknown[]> = {
  area: string;
  operation: string;
  thresholdMs?: number;
  normalizeArgs?: (...args: TArgs) => TArgs;
  getContext: (...args: TArgs) => Record<string, unknown>;
  load: (...args: TArgs) => Promise<SeoMetadataPayload>;
};

export function createCachedObservedSeoMetadataLoader<TArgs extends unknown[]>({
  area,
  operation,
  thresholdMs = 150,
  normalizeArgs,
  getContext,
  load,
}: CreateSeoLoaderOptions<TArgs>) {
  return createCachedObservedLoader({
    area,
    operation,
    thresholdMs,
    normalizeArgs,
    getContext: (...args: TArgs) =>
      buildSeoLoaderObservationContext(area, getContext(...args), {
        hasCanonicalNormalization: Boolean(normalizeArgs),
      }),
    getSuccessContext: (result, ...args: TArgs) => {
      void args;
      return buildSeoLoaderSuccessContext(area, result, {
        hasCanonicalNormalization: Boolean(normalizeArgs),
      });
    },
    load,
  });
}
