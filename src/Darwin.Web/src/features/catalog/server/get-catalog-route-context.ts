import "server-only";
import { getCatalogBrowseContext } from "@/features/catalog/server/get-catalog-browse-context";
import { getProductDetailContext } from "@/features/catalog/server/get-product-detail-context";
import { getPublicStorefrontContext } from "@/features/storefront/server/get-public-storefront-context";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeCatalogRouteHealth } from "@/lib/route-health";
import {
  readCatalogMediaState,
  readCatalogSavingsBand,
  readCatalogVisibleSort,
  readCatalogVisibleState,
} from "@/features/catalog/discovery";
import {
  catalogIndexRouteObservationContext,
  productDetailRouteObservationContext,
} from "@/lib/route-observation-context";

function normalizeCatalogIndexArgs(
  culture: string,
  page: number,
  categorySlug?: string,
  search?: string,
): [string, number, string | undefined, string | undefined] {
  return [
    culture,
    Number.isFinite(page) && page > 0 ? Math.floor(page) : 1,
    categorySlug?.trim() || undefined,
    search?.trim() || undefined,
  ];
}

function normalizeCatalogDetailArgs(
  culture: string,
  slug: string,
  category?: string,
  visibleQuery?: string,
  visibleState?: string,
  visibleSort?: string,
  mediaState?: string,
  savingsBand?: string,
): [
  string,
  string,
  string | undefined,
  string | undefined,
  string | undefined,
  string | undefined,
  string | undefined,
  string | undefined,
] {
  return [
    culture,
    slug,
    category?.trim() || undefined,
    visibleQuery?.trim() || undefined,
    readCatalogVisibleState(visibleState),
    readCatalogVisibleSort(visibleSort),
    readCatalogMediaState(mediaState),
    readCatalogSavingsBand(savingsBand),
  ];
}

const getCachedCatalogIndexRouteContext = createCachedObservedLoader({
  area: "catalog",
  operation: "load-route-context",
  thresholdMs: 325,
  normalizeArgs: normalizeCatalogIndexArgs,
  getContext: (
    culture: string,
    page: number,
    categorySlug?: string,
    search?: string,
  ) => catalogIndexRouteObservationContext(culture, page, categorySlug, search),
  getSuccessContext: summarizeCatalogRouteHealth,
  load: async (
    culture: string,
    page: number,
    categorySlug?: string,
    search?: string,
  ) => {
    const [browseContext, storefrontContext] = await Promise.all([
      getCatalogBrowseContext(culture, page, categorySlug, search),
      getPublicStorefrontContext(culture),
    ]);

    return {
      browseContext,
      storefrontContext,
    };
  },
});

const getCachedCatalogDetailRouteContext = createCachedObservedLoader({
  area: "product-detail",
  operation: "load-route-context",
  thresholdMs: 325,
  normalizeArgs: normalizeCatalogDetailArgs,
  getContext: (
    culture: string,
    slug: string,
    category?: string,
    visibleQuery?: string,
    visibleState?: string,
    visibleSort?: string,
    mediaState?: string,
    savingsBand?: string,
  ) => ({
    ...productDetailRouteObservationContext(culture, slug),
    categorySlug: category ?? null,
    visibleQuery: visibleQuery ?? null,
    visibleState: visibleState ?? null,
    visibleSort: visibleSort ?? null,
    mediaState: mediaState ?? null,
    savingsBand: savingsBand ?? null,
  }),
  getSuccessContext: summarizeCatalogRouteHealth,
  load: async (
    culture: string,
    slug: string,
    category?: string,
    visibleQuery?: string,
    visibleState?: string,
    visibleSort?: string,
    mediaState?: string,
    savingsBand?: string,
  ) => {
    const [detailContext, storefrontContext] = await Promise.all([
      getProductDetailContext(culture, slug, {
        category,
        visibleQuery,
        visibleState,
        visibleSort,
        mediaState,
        savingsBand,
      }),
      getPublicStorefrontContext(culture),
    ]);

    return {
      detailContext,
      storefrontContext,
    };
  },
});

export async function getCatalogIndexRouteContext(
  culture: string,
  page: number,
  categorySlug?: string,
  search?: string,
) {
  return getCachedCatalogIndexRouteContext(culture, page, categorySlug, search);
}

export async function getCatalogDetailRouteContext(
  culture: string,
  slug: string,
  category?: string,
  visibleQuery?: string,
  visibleState?: string,
  visibleSort?: string,
  mediaState?: string,
  savingsBand?: string,
) {
  return getCachedCatalogDetailRouteContext(
    culture,
    slug,
    category,
    visibleQuery,
    visibleState,
    visibleSort,
    mediaState,
    savingsBand,
  );
}
