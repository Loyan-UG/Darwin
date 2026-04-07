import type { Metadata } from "next";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeSeoMetadataHealth } from "@/lib/route-health";

export type SeoMetadataPayload = {
  metadata: Metadata;
  canonicalPath: string;
  noIndex: boolean;
  languageAlternates?: Record<string, string>;
};

export function buildSeoLoaderObservationContext(
  area: string,
  context?: Record<string, unknown>,
) {
  return {
    pageLoaderKind: "seo-metadata",
    seoArea: area,
    ...(context ?? {}),
  };
}

export function buildSeoLoaderSuccessContext(
  area: string,
  result: SeoMetadataPayload,
) {
  return {
    pageLoaderKind: "seo-metadata",
    seoArea: area,
    indexability: result.noIndex ? "noindex" : "indexable",
    ...summarizeSeoMetadataHealth(result),
  };
}

type CreateSeoLoaderOptions<TArgs extends unknown[]> = {
  area: string;
  operation: string;
  thresholdMs?: number;
  getContext: (...args: TArgs) => Record<string, unknown>;
  load: (...args: TArgs) => Promise<SeoMetadataPayload>;
};

export function createCachedObservedSeoMetadataLoader<TArgs extends unknown[]>({
  area,
  operation,
  thresholdMs = 150,
  getContext,
  load,
}: CreateSeoLoaderOptions<TArgs>) {
  return createCachedObservedLoader({
    area,
    operation,
    thresholdMs,
    getContext: (...args: TArgs) =>
      buildSeoLoaderObservationContext(area, getContext(...args)),
    getSuccessContext: (result, ...args: TArgs) => {
      void args;
      return buildSeoLoaderSuccessContext(area, result);
    },
    load,
  });
}
