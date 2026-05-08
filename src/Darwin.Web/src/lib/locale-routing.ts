import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import { buildQuerySuffix, serializeQueryParams } from "@/lib/query-params";

export const REQUEST_CULTURE_HEADER = "x-darwin-request-culture";
export const REQUEST_PATHNAME_HEADER = "x-darwin-request-pathname";
export const INFERRED_CULTURE_SEARCH_PARAM = "_darwinInferredCulture";

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

export function stripCulturePrefix(
  pathname: string,
  supportedCultures = getSiteRuntimeConfig().supportedCultures,
) {
  const normalizedPath = normalizePathname(pathname);
  if (normalizedPath === "/") {
    return {
      culture: null,
      pathname: normalizedPath,
    };
  }

  const segments = normalizedPath.split("/");
  const possibleCulture = segments[1];

  if (!supportedCultures.includes(possibleCulture)) {
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
    isExactOrChildPath(strippedPath, "/cms") ||
    isExactOrChildPath(strippedPath, "/help") ||
    isExactOrChildPath(strippedPath, "/page") ||
    isExactOrChildPath(strippedPath, "/account") ||
    isExactOrChildPath(strippedPath, "/cart") ||
    isExactOrChildPath(strippedPath, "/checkout") ||
    isExactOrChildPath(strippedPath, "/invoices") ||
    isExactOrChildPath(strippedPath, "/loyalty") ||
    isExactOrChildPath(strippedPath, "/orders")
  );
}

export function buildLocalizedPath(pathname: string, culture: string) {
  void culture;
  const normalizedPath = stripCulturePrefix(pathname).pathname;

  if (!isPublicLocalizedPath(normalizedPath)) {
    return normalizedPath;
  }

  return normalizedPath;
}

export function localizeHref(href: string, culture: string) {
  if (!href || isExternalHref(href)) {
    return href;
  }

  const { pathname, search, hash } = splitHref(href);
  const localizedPath = buildLocalizedPath(pathname || "/", culture);
  return `${localizedPath}${search}${hash}`;
}

export function buildLocalizedAuthHref(
  authPath: string,
  returnPath: string | undefined | null,
  culture: string,
  fallback = "/account",
) {
  const safeReturnPath = sanitizeAppPath(returnPath, fallback);
  return localizeHref(
    `${authPath}?returnPath=${encodeURIComponent(safeReturnPath)}`,
    culture,
  );
}

export function buildLocalizedQueryHref(
  pathname: string,
  params: Record<string, string | number | undefined | null>,
  culture: string,
) {
  return localizeHref(`${pathname}${buildQuerySuffix(params)}`, culture);
}

export function buildAppQueryPath(
  pathname: string,
  params: Record<string, string | number | undefined | null>,
) {
  return `${pathname}${buildQuerySuffix(params)}`;
}

export function appendAppQueryParam(
  path: string,
  key: string,
  value: string | number | undefined | null,
) {
  if (value === undefined || value === null || value === "") {
    return path;
  }

  const separator = path.includes("?") ? "&" : "?";
  return `${path}${separator}${serializeQueryParams({ [key]: value })}`;
}
