export type PublicApiCachePolicy = {
  revalidate: number;
  tags: string[];
};

export type PublicApiCacheIdentity = PublicApiCachePolicy & {
  normalizedPath: string;
  keyTag: string;
  pathTag: string;
};

export type PublicApiFetchCacheOptions =
  | {
      cache: "force-cache";
      next: {
        revalidate: number;
        tags: string[];
      };
    }
  | {
      cache: "no-store";
    };

export type PublicApiRequestPlan = {
  requestUrl: string;
  cacheIdentity: PublicApiCacheIdentity;
  fetchCacheOptions: PublicApiFetchCacheOptions;
};

const PUBLIC_API_REVALIDATE_BY_KEY: Record<string, number> = {
  "cms-menu": 900,
  "cms-page": 300,
  "cms-pages": 180,
  "catalog-categories": 900,
  "catalog-products": 120,
  "catalog-product-detail": 180,
};

export function getPublicApiRevalidate(key: string) {
  return PUBLIC_API_REVALIDATE_BY_KEY[key] ?? 60;
}

export function getPublicApiKeyTag(key: string) {
  return `public:${key}`;
}

function hasQueryParam(path: string, key: string) {
  const [, rawQuery] = path.split("?", 2);
  if (!rawQuery) {
    return false;
  }

  return new URLSearchParams(rawQuery).has(key);
}

function getNumericQueryParam(path: string, key: string) {
  const [, rawQuery] = path.split("?", 2);
  if (!rawQuery) {
    return undefined;
  }

  const rawValue = new URLSearchParams(rawQuery).get(key);
  if (!rawValue) {
    return undefined;
  }

  const value = Number(rawValue);
  return Number.isFinite(value) ? value : undefined;
}

function getPublicApiPathAwareRevalidate(key: string, path: string) {
  const pageSize = getNumericQueryParam(path, "pageSize");

  if (key === "cms-page") {
    return 180;
  }

  if (key === "catalog-product-detail") {
    return 120;
  }

  if (
    key === "cms-pages" &&
    (hasQueryParam(path, "search") || (pageSize !== undefined && pageSize > 24))
  ) {
    return 120;
  }

  if (
    key === "catalog-products" &&
    (hasQueryParam(path, "search") ||
      hasQueryParam(path, "categorySlug") ||
      hasQueryParam(path, "visibleState") ||
      hasQueryParam(path, "savingsBand") ||
      (pageSize !== undefined && pageSize > 24))
  ) {
    return 90;
  }

  return getPublicApiRevalidate(key);
}

export function normalizePublicApiCachePath(path: string) {
  const [pathname, rawQuery] = path.split("?", 2);
  if (!rawQuery) {
    return pathname;
  }

  const params = new URLSearchParams(rawQuery);
  const orderedEntries = Array.from(params.entries()).sort(
    ([leftKey, leftValue], [rightKey, rightValue]) =>
      leftKey === rightKey
        ? leftValue.localeCompare(rightValue)
        : leftKey.localeCompare(rightKey),
  );
  const normalizedParams = new URLSearchParams();

  orderedEntries.forEach(([key, value]) =>
    normalizedParams.append(key, value),
  );

  const normalizedQuery = normalizedParams.toString();
  return normalizedQuery ? `${pathname}?${normalizedQuery}` : pathname;
}

export function getPublicApiPathTag(path: string) {
  return `path:${normalizePublicApiCachePath(path)}`;
}

export function getPublicApiCacheIdentity(
  key: string,
  path: string,
): PublicApiCacheIdentity {
  const normalizedPath = normalizePublicApiCachePath(path);
  const revalidate = getPublicApiPathAwareRevalidate(key, normalizedPath);
  const keyTag = getPublicApiKeyTag(key);
  const pathTag = getPublicApiPathTag(normalizedPath);

  return {
    normalizedPath,
    revalidate,
    keyTag,
    pathTag,
    tags: [keyTag, pathTag],
  };
}

export function getPublicApiCachePolicy(
  key: string,
  path: string,
): PublicApiCachePolicy {
  const { revalidate, tags } = getPublicApiCacheIdentity(key, path);

  return {
    revalidate,
    tags,
  };
}

export function getPublicApiFetchCacheOptions(
  identity: PublicApiCacheIdentity,
  method?: string,
): PublicApiFetchCacheOptions {
  if (method && method !== "GET") {
    return {
      cache: "no-store",
    };
  }

  return {
    cache: "force-cache",
    next: {
      revalidate: identity.revalidate,
      tags: identity.tags,
    },
  };
}

export function getPublicApiRequestPlan(
  webApiBaseUrl: string,
  key: string,
  path: string,
  method?: string,
): PublicApiRequestPlan {
  const cacheIdentity = getPublicApiCacheIdentity(key, path);

  return {
    requestUrl: `${webApiBaseUrl}${cacheIdentity.normalizedPath}`,
    cacheIdentity,
    fetchCacheOptions: getPublicApiFetchCacheOptions(cacheIdentity, method),
  };
}


