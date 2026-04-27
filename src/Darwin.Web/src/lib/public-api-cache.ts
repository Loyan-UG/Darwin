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

export type PublicApiPathCharacteristics = {
  hasSearch: boolean;
  hasCategorySlug: boolean;
  hasVisibleState: boolean;
  hasVisibleSort: boolean;
  hasMediaState: boolean;
  hasSavingsBand: boolean;
  page?: number;
  pageSize?: number;
};

export type PublicApiCacheProfile =
  | "stable"
  | "detail"
  | "category-heavy"
  | "discovery-filtered"
  | "discovery-paged"
  | "default";

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

export function getPublicApiPathCharacteristics(
  path: string,
): PublicApiPathCharacteristics {
  return {
    hasSearch: hasQueryParam(path, "search"),
    hasCategorySlug: hasQueryParam(path, "categorySlug"),
    hasVisibleState: hasQueryParam(path, "visibleState"),
    hasVisibleSort: hasQueryParam(path, "visibleSort"),
    hasMediaState: hasQueryParam(path, "mediaState"),
    hasSavingsBand: hasQueryParam(path, "savingsBand"),
    page: getNumericQueryParam(path, "page"),
    pageSize: getNumericQueryParam(path, "pageSize"),
  };
}

export function getPublicApiCacheProfile(
  key: string,
  path: string,
): PublicApiCacheProfile {
  const characteristics = getPublicApiPathCharacteristics(path);
  const { page, pageSize } = characteristics;

  if (key === "cms-page") {
    return "detail";
  }

  if (key === "catalog-product-detail") {
    return "detail";
  }

  if (
    key === "catalog-categories" &&
    ((pageSize !== undefined && pageSize > 24) || (page !== undefined && page > 1))
  ) {
    return "category-heavy";
  }

  if (
    key === "cms-pages" &&
    characteristics.hasSearch
  ) {
    return "discovery-filtered";
  }

  if (
    key === "cms-pages" &&
    ((pageSize !== undefined && pageSize > 24) ||
      (page !== undefined && page > 1))
  ) {
    return "discovery-paged";
  }

  if (
    key === "catalog-products" &&
    (characteristics.hasSearch ||
      characteristics.hasCategorySlug ||
      characteristics.hasVisibleState ||
      characteristics.hasVisibleSort ||
      characteristics.hasMediaState ||
      characteristics.hasSavingsBand ||
      characteristics.hasCategorySlug)
  ) {
    return "discovery-filtered";
  }

  if (
    key === "catalog-products" &&
    ((pageSize !== undefined && pageSize > 24) ||
      (page !== undefined && page > 1))
  ) {
    return "discovery-paged";
  }

  if (key === "cms-menu" || key === "catalog-categories") {
    return "stable";
  }

  return "default";
}

export function getPublicApiProfileRevalidate(
  key: string,
  profile: PublicApiCacheProfile,
) {
  switch (profile) {
    case "stable":
      return getPublicApiRevalidate(key);
    case "detail":
      return key === "catalog-product-detail" ? 120 : 180;
    case "category-heavy":
      return 300;
    case "discovery-filtered":
      return key === "cms-pages" ? 120 : 90;
    case "discovery-paged":
      return key === "cms-pages" ? 120 : key === "catalog-categories" ? 300 : 90;
    default:
      return getPublicApiRevalidate(key);
  }
}

function getPublicApiPathAwareRevalidate(key: string, path: string) {
  const profile = getPublicApiCacheProfile(key, path);
  return getPublicApiProfileRevalidate(key, profile);
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
  if (method && method.toUpperCase() !== "GET") {
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






