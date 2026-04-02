import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

export const REQUEST_CULTURE_HEADER = "x-darwin-request-culture";

function normalizePathname(pathname: string) {
  if (!pathname || pathname === "/") {
    return "/";
  }

  const normalized = pathname.startsWith("/") ? pathname : `/${pathname}`;
  return normalized.endsWith("/") ? normalized.slice(0, -1) : normalized;
}

function isExactOrChildPath(pathname: string, basePath: string) {
  return pathname === basePath || pathname.startsWith(`${basePath}/`);
}

function isExternalHref(href: string) {
  return /^(?:[a-z][a-z0-9+.-]*:|\/\/|#)/i.test(href);
}

function splitHref(href: string) {
  const hashIndex = href.indexOf("#");
  const hash = hashIndex >= 0 ? href.slice(hashIndex) : "";
  const beforeHash = hashIndex >= 0 ? href.slice(0, hashIndex) : href;
  const queryIndex = beforeHash.indexOf("?");

  return {
    pathname: queryIndex >= 0 ? beforeHash.slice(0, queryIndex) : beforeHash,
    search: queryIndex >= 0 ? beforeHash.slice(queryIndex) : "",
    hash,
  };
}

export function sanitizeAppPath(value: string | undefined | null, fallback = "/") {
  const normalizedFallback = sanitizeFallback(fallback);
  const trimmed = value?.trim();

  if (!trimmed || isExternalHref(trimmed)) {
    return normalizedFallback;
  }

  const { pathname, search, hash } = splitHref(trimmed);
  if (!pathname.startsWith("/") || pathname.startsWith("//")) {
    return normalizedFallback;
  }

  return `${pathname}${search}${hash}`;
}

function sanitizeFallback(fallback: string) {
  const trimmed = fallback.trim();
  return trimmed.startsWith("/") && !trimmed.startsWith("//") ? trimmed : "/";
}

export function stripCulturePrefix(pathname: string) {
  const normalizedPath = normalizePathname(pathname);
  if (normalizedPath === "/") {
    return {
      culture: null,
      pathname: normalizedPath,
    };
  }

  const runtimeConfig = getSiteRuntimeConfig();
  const segments = normalizedPath.split("/");
  const possibleCulture = segments[1];

  if (!runtimeConfig.supportedCultures.includes(possibleCulture)) {
    return {
      culture: null,
      pathname: normalizedPath,
    };
  }

  const remainingPath = segments.slice(2).join("/");

  return {
    culture: possibleCulture,
    pathname: remainingPath ? `/${remainingPath}` : "/",
  };
}

export function isPublicLocalizedPath(pathname: string) {
  const strippedPath = stripCulturePrefix(pathname).pathname;

  return (
    strippedPath === "/" ||
    isExactOrChildPath(strippedPath, "/catalog") ||
    isExactOrChildPath(strippedPath, "/cms")
  );
}

export function buildLocalizedPath(pathname: string, culture: string) {
  const runtimeConfig = getSiteRuntimeConfig();
  const normalizedPath = stripCulturePrefix(pathname).pathname;

  if (!isPublicLocalizedPath(normalizedPath)) {
    return normalizedPath;
  }

  if (culture === runtimeConfig.defaultCulture) {
    return normalizedPath;
  }

  return normalizedPath === "/"
    ? `/${culture}`
    : `/${culture}${normalizedPath}`;
}

export function localizeHref(href: string, culture: string) {
  if (!href || isExternalHref(href)) {
    return href;
  }

  const { pathname, search, hash } = splitHref(href);
  const localizedPath = buildLocalizedPath(pathname || "/", culture);
  return `${localizedPath}${search}${hash}`;
}
