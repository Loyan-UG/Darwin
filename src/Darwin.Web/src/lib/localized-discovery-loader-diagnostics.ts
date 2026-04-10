type LocalizedDiscoveryLoaderKind = "inventory" | "sitemap";

export function getLocalizedDiscoveryNormalizationMode(
  hasCanonicalCultureNormalization?: boolean,
) {
  return hasCanonicalCultureNormalization
    ? "canonical-cultures"
    : "raw-cultures";
}

export function buildLocalizedDiscoveryState(extras?: Record<string, unknown>) {
  if (
    extras?.localizedDiscoveryState === "present" ||
    extras?.localizedDiscoveryState === "empty" ||
    extras?.localizedDiscoveryState === "unknown"
  ) {
    return extras.localizedDiscoveryState;
  }

  const numericSignals = [
    extras?.localizedCultureCount,
    extras?.localizedItemCount,
    extras?.localizedPageCount,
    extras?.localizedProductCount,
    extras?.totalEntryCount,
  ].filter((value): value is number => typeof value === "number");

  if (numericSignals.length === 0) {
    return "unknown";
  }

  return numericSignals.some((value) => value > 0) ? "present" : "empty";
}

export function buildLocalizedDiscoverySummaryFootprint(
  extras?: Record<string, unknown>,
) {
  const footprint =
    extras?.localizedDiscoverySummaryFootprint ??
    extras?.sitemapSummaryFootprint ??
    extras?.localizedInventorySummaryFootprint ??
    extras?.alternateMapSummaryFootprint ??
    extras?.localizedDiscoveryFootprint ??
    extras?.sitemapCompositionFootprint ??
    extras?.localizedInventoryFootprint ??
    extras?.alternateMapFootprint;

  return typeof footprint === "string" && footprint.length > 0
    ? footprint
    : "summary:none";
}

export function buildLocalizedDiscoveryDetailFootprint(
  extras?: Record<string, unknown>,
) {
  const footprint =
    extras?.localizedDiscoveryDetailFootprint ??
    extras?.localizedDiscoveryFootprint ??
    extras?.sitemapCompositionFootprint ??
    extras?.localizedInventoryFootprint ??
    extras?.alternateMapFootprint;

  return typeof footprint === "string" && footprint.length > 0
    ? footprint
    : "detail:none";
}

export function buildLocalizedDiscoveryLoaderBaseDiagnostics(
  kind: LocalizedDiscoveryLoaderKind,
  options?: {
    hasCanonicalCultureNormalization?: boolean;
    extras?: Record<string, unknown>;
  },
) {
  return {
    localizedDiscoveryKind: kind,
    localizedDiscoveryNormalization: getLocalizedDiscoveryNormalizationMode(
      options?.hasCanonicalCultureNormalization,
    ),
    ...(options?.extras ?? {}),
  };
}

export function buildLocalizedDiscoveryLoaderSuccessDiagnostics(
  kind: LocalizedDiscoveryLoaderKind,
  extras?: Record<string, unknown>,
  options?: {
    hasCanonicalCultureNormalization?: boolean;
  },
) {
  return buildLocalizedDiscoveryLoaderBaseDiagnostics(kind, {
    hasCanonicalCultureNormalization:
      options?.hasCanonicalCultureNormalization,
    extras: {
      localizedDiscoveryState: buildLocalizedDiscoveryState(extras),
      localizedDiscoveryDetailFootprint:
        buildLocalizedDiscoveryDetailFootprint(extras),
      localizedDiscoverySummaryFootprint:
        buildLocalizedDiscoverySummaryFootprint(extras),
      ...(extras ?? {}),
    },
  });
}
