import sharedDe from "@/localization/resources/shared.de-DE.json";
import sharedEn from "@/localization/resources/shared.en-US.json";
import shellDe from "@/localization/resources/shell.de-DE.json";
import shellEn from "@/localization/resources/shell.en-US.json";
import catalogDe from "@/localization/resources/catalog.de-DE.json";
import catalogEn from "@/localization/resources/catalog.en-US.json";
import commerceDe from "@/localization/resources/commerce.de-DE.json";
import commerceEn from "@/localization/resources/commerce.en-US.json";
import memberDe from "@/localization/resources/member.de-DE.json";
import memberEn from "@/localization/resources/member.en-US.json";
import homeDe from "@/localization/resources/home.de-DE.json";
import homeEn from "@/localization/resources/home.en-US.json";

const QUERY_LOCALIZATION_PREFIX = "i18n:";

const sharedBundles = {
  "de-DE": sharedDe,
  "en-US": sharedEn,
} as const;

const shellBundles = {
  "de-DE": shellDe,
  "en-US": shellEn,
} as const;

const catalogBundles = {
  "de-DE": catalogDe,
  "en-US": catalogEn,
} as const;

const commerceBundles = {
  "de-DE": commerceDe,
  "en-US": commerceEn,
} as const;

const memberBundles = {
  "de-DE": memberDe,
  "en-US": memberEn,
} as const;

const homeBundles = {
  "de-DE": homeDe,
  "en-US": homeEn,
} as const;

const bundleRegistries = [
  sharedBundles,
  shellBundles,
  catalogBundles,
  commerceBundles,
  memberBundles,
  homeBundles,
] as const;

function resolveBundleCulture<T extends Record<string, unknown>>(
  culture: string,
  bundles: T,
) {
  if (culture in bundles) {
    return culture as keyof T;
  }

  const language = culture.split("-")[0]?.toLowerCase();
  const fallbackKey = Object.keys(bundles).find((key) =>
    key.toLowerCase().startsWith(`${language}-`),
  );

  return (fallbackKey ?? "de-DE") as keyof T;
}

export function getSharedResource(culture: string) {
  return sharedBundles[resolveBundleCulture(culture, sharedBundles)];
}

export function getShellResource(culture: string) {
  return shellBundles[resolveBundleCulture(culture, shellBundles)];
}

export function getCatalogResource(culture: string) {
  return catalogBundles[resolveBundleCulture(culture, catalogBundles)];
}

export function getCommerceResource(culture: string) {
  return commerceBundles[resolveBundleCulture(culture, commerceBundles)];
}

export function getMemberResource(culture: string) {
  return memberBundles[resolveBundleCulture(culture, memberBundles)];
}

export function getHomeResource(culture: string) {
  return homeBundles[resolveBundleCulture(culture, homeBundles)];
}

export function formatResource(
  template: string,
  values: Record<string, string | number>,
) {
  return template.replace(/\{(\w+)\}/g, (_, key: string) =>
    String(values[key] ?? `{${key}}`),
  );
}

export function toLocalizedQueryMessage(key: string) {
  return `${QUERY_LOCALIZATION_PREFIX}${key}`;
}

export function isLocalizedQueryMessage(value: string | undefined) {
  return Boolean(value?.startsWith(QUERY_LOCALIZATION_PREFIX));
}

export function matchesLocalizedQueryMessageKey(
  value: string | undefined,
  key: string,
  legacyValue?: string,
) {
  return (
    value === toLocalizedQueryMessage(key) ||
    value === legacyValue ||
    value === key
  );
}

export function resolveProblemQueryMessage(
  problem: { detail?: string; title?: string } | null | undefined,
  fallbackKey: string,
): string {
  const fallbackMessage = toLocalizedQueryMessage(fallbackKey);
  const detail = problem?.detail?.trim();
  const title = problem?.title?.trim();

  if (isLocalizedQueryMessage(detail)) {
    return detail ?? fallbackMessage;
  }

  if (isLocalizedQueryMessage(title)) {
    return title ?? fallbackMessage;
  }

  return fallbackMessage;
}

function resolveApiStatusLabelKey(status: string | undefined) {
  switch (status) {
    case "ok":
      return "publicApiStatusOkLabel";
    case "not-found":
      return "publicApiStatusNotFoundLabel";
    case "network-error":
      return "publicApiStatusNetworkErrorLabel";
    case "http-error":
      return "publicApiStatusHttpErrorLabel";
    case "invalid-payload":
      return "publicApiStatusInvalidPayloadLabel";
    case "unauthorized":
      return "publicApiStatusUnauthorizedLabel";
    case "unauthenticated":
      return "publicApiStatusUnauthenticatedLabel";
    default:
      return undefined;
  }
}

export function resolveApiStatusLabel<T extends Record<string, unknown>>(
  status: string | undefined,
  bundle: T,
) {
  const key = resolveApiStatusLabelKey(status);
  if (!key) {
    return status;
  }

  return resolveLocalizedQueryMessage(toLocalizedQueryMessage(key), bundle) ?? status;
}

export function resolveStatusMappedMessage<T extends Record<string, unknown>>(
  status: string | undefined,
  bundle: T,
  statusMessageKeys: Partial<Record<string, string>>,
) {
  const key = status ? statusMessageKeys[status] : undefined;
  if (!key) {
    return undefined;
  }

  return resolveLocalizedQueryMessage(toLocalizedQueryMessage(key), bundle);
}

function resolveResourceCulture(bundle: Record<string, unknown>) {
  for (const registry of bundleRegistries) {
    for (const [culture, resource] of Object.entries(registry)) {
      if (resource === bundle) {
        return culture;
      }
    }
  }

  return null;
}

export function resolveLocalizedQueryMessage<T extends Record<string, unknown>>(
  value: string | undefined,
  bundle: T,
) {
  if (!isLocalizedQueryMessage(value)) {
    return value;
  }

  const localizedKeyValue = value ?? "";
  const key = localizedKeyValue.slice(QUERY_LOCALIZATION_PREFIX.length) as keyof T;
  const localizedValue = bundle[key];
  if (typeof localizedValue === "string") {
    return localizedValue;
  }

  const culture = resolveResourceCulture(bundle);
  if (!culture) {
    return value;
  }

  for (const registry of bundleRegistries) {
    const localizedFallback = (registry as Record<string, Record<string, unknown>>)[culture]?.[
      key as string
    ];

    if (typeof localizedFallback === "string") {
      return localizedFallback;
    }
  }

  return value;
}
