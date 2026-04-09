import type { SeoMetadataPayload } from "@/lib/seo-loader";
import { summarizeSeoMetadataHealth } from "@/lib/route-health";

export function buildSeoLoaderBaseDiagnostics(
  area: string,
  options?: {
    hasCanonicalNormalization?: boolean;
    extras?: Record<string, unknown>;
  },
) {
  return {
    pageLoaderKind: "seo-metadata",
    seoArea: area,
    seoNormalization: options?.hasCanonicalNormalization ? "canonical" : "raw",
    ...(options?.extras ?? {}),
  };
}

export function buildSeoLanguageAlternateFootprint(
  languageAlternates?: Record<string, string>,
) {
  return summarizeSeoMetadataHealth({
    canonicalPath: "",
    noIndex: false,
    languageAlternates,
  }).seoAlternateSummaryFootprint;
}

export function buildSeoLanguageAlternateDetailFootprint(
  languageAlternates?: Record<string, string>,
) {
  return summarizeSeoMetadataHealth({
    canonicalPath: "",
    noIndex: false,
    languageAlternates,
  }).seoAlternateDetailFootprint;
}

export function buildSeoMetadataState(result: {
  noIndex: boolean;
  languageAlternates?: Record<string, string>;
}) {
  return summarizeSeoMetadataHealth({
    canonicalPath: "",
    noIndex: result.noIndex,
    languageAlternates: result.languageAlternates,
  }).seoMetadataState;
}

export function buildSeoIndexability(result: { noIndex: boolean }) {
  return summarizeSeoMetadataHealth({
    canonicalPath: "",
    noIndex: result.noIndex,
  }).seoIndexability;
}

export function buildSeoLanguageAlternateState(
  languageAlternates?: Record<string, string>,
) {
  return summarizeSeoMetadataHealth({
    canonicalPath: "",
    noIndex: false,
    languageAlternates,
  }).languageAlternateState;
}

export function buildSeoVisibilityFootprint(result: {
  noIndex: boolean;
  languageAlternates?: Record<string, string>;
}) {
  return summarizeSeoMetadataHealth({
    canonicalPath: "",
    noIndex: result.noIndex,
    languageAlternates: result.languageAlternates,
  }).seoVisibilityFootprint;
}

export function buildSeoSummaryFootprint(result: {
  noIndex: boolean;
  languageAlternates?: Record<string, string>;
}) {
  return summarizeSeoMetadataHealth({
    canonicalPath: "",
    noIndex: result.noIndex,
    languageAlternates: result.languageAlternates,
  }).seoSummaryFootprint;
}

export function buildSeoTargetFootprint(result: {
  canonicalPath: string;
  noIndex: boolean;
  languageAlternates?: Record<string, string>;
}) {
  return summarizeSeoMetadataHealth({
    canonicalPath: result.canonicalPath,
    noIndex: result.noIndex,
    languageAlternates: result.languageAlternates,
  }).seoTargetFootprint;
}

export function buildSeoSuccessDiagnostics(
  area: string,
  result: SeoMetadataPayload,
  options?: {
    hasCanonicalNormalization?: boolean;
  },
) {
  const seoHealth = summarizeSeoMetadataHealth(result);

  return buildSeoLoaderBaseDiagnostics(area, {
    hasCanonicalNormalization: options?.hasCanonicalNormalization,
    extras: {
      indexability: buildSeoIndexability(result),
      seoMetadataState: buildSeoMetadataState(result),
      seoVisibilityFootprint: buildSeoVisibilityFootprint(result),
      seoTargetFootprint: buildSeoTargetFootprint(result),
      languageAlternateState:
        buildSeoLanguageAlternateState(result.languageAlternates),
      languageAlternateFootprint:
        buildSeoLanguageAlternateDetailFootprint(result.languageAlternates),
      seoAlternateSummaryFootprint: seoHealth.seoAlternateSummaryFootprint,
      seoSummaryFootprint: buildSeoSummaryFootprint(result),
    },
  });
}
