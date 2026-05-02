import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

export type PublicSiteRuntimeConfig = {
  defaultCulture: string;
  supportedCultures: string[];
  multilingualEnabled: boolean;
};

export function normalizeSupportedCultures(
  defaultCulture: string,
  supportedCultures: string[],
) {
  const normalizedDefault = defaultCulture.trim();
  const normalizedCultures = supportedCultures
    .map((culture) => culture.trim())
    .filter(Boolean);
  const distinctCultures = Array.from(new Set([
    normalizedDefault,
    ...normalizedCultures,
  ].filter(Boolean)));

  return distinctCultures.length > 0 ? distinctCultures : ["de-DE"];
}

export function getFallbackPublicSiteRuntimeConfig(): PublicSiteRuntimeConfig {
  const runtimeConfig = getSiteRuntimeConfig();
  const supportedCultures = normalizeSupportedCultures(
    runtimeConfig.defaultCulture,
    runtimeConfig.supportedCultures,
  );

  return {
    defaultCulture: supportedCultures.includes(runtimeConfig.defaultCulture)
      ? runtimeConfig.defaultCulture
      : supportedCultures[0]!,
    supportedCultures,
    multilingualEnabled: supportedCultures.length > 1,
  };
}

export function normalizePublicSiteRuntimeConfig(
  value: PublicSiteRuntimeConfig,
): PublicSiteRuntimeConfig {
  const fallback = getFallbackPublicSiteRuntimeConfig();
  const supportedCultures = normalizeSupportedCultures(
    value.defaultCulture || fallback.defaultCulture,
    value.supportedCultures.length > 0
      ? value.supportedCultures
      : fallback.supportedCultures,
  );
  const defaultCulture = supportedCultures.includes(value.defaultCulture)
    ? value.defaultCulture
    : supportedCultures[0]!;

  return {
    defaultCulture,
    supportedCultures,
    multilingualEnabled: supportedCultures.length > 1,
  };
}

export function isPublicSiteRuntimeConfig(
  value: unknown,
): value is PublicSiteRuntimeConfig {
  if (!value || typeof value !== "object") {
    return false;
  }

  const candidate = value as Partial<PublicSiteRuntimeConfig>;
  return (
    typeof candidate.defaultCulture === "string" &&
    Array.isArray(candidate.supportedCultures) &&
    candidate.supportedCultures.every((culture) => typeof culture === "string")
  );
}
