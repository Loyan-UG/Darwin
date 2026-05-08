import "server-only";
import {
  buildCatalogVisibleWindow,
  readCatalogMediaState,
  readCatalogSavingsBand,
  readCatalogVisibleSort,
  readCatalogVisibleState,
  summarizeCatalogFacets,
} from "@/features/catalog/discovery";
import { getCatalogBrowseSet } from "@/features/catalog/server/get-catalog-browse-set";
import {
  getCatalogDetailRouteContext,
  getCatalogIndexRouteContext,
} from "@/features/catalog/server/get-catalog-route-context";
import { createPublicDiscoveryPageLoader } from "@/features/storefront/server/create-public-discovery-page-loader";
import {
  summarizeCatalogIndexPageHealth,
  summarizeCatalogRouteHealth,
} from "@/lib/route-health";

type CatalogIndexContextFootprintSource = {
  cmsPagesResult?: { status: string; data?: { items?: Array<unknown> } | null } | null;
  productsResult?: { status: string; data?: { items?: Array<unknown> } | null } | null;
  cartSummary?: { status: string } | null;
};

export function summarizeCatalogIndexContextFootprint(
  result: CatalogIndexContextFootprintSource,
) {
  return `cms:${result.cmsPagesResult?.status ?? "unknown"}:${result.cmsPagesResult?.data?.items?.length ?? 0}|products:${result.productsResult?.status ?? "unknown"}:${result.productsResult?.data?.items?.length ?? 0}|cart:${result.cartSummary?.status ?? "missing"}`;
}

const getCachedCatalogIndexPageContext = createPublicDiscoveryPageLoader({
  area: "catalog-page-context",
  operation: "load-index-page-context",
  thresholdMs: 300,
  getContext: (
    culture: string,
    page: number,
    categorySlug?: string,
    search?: string,
    visibleState?: string,
    visibleSort?: string,
    mediaState?: string,
    savingsBand?: string,
  ) => ({
    culture,
    route: "/catalog",
    page,
    categorySlug: categorySlug ?? null,
    search: search ?? null,
    visibleState: visibleState ?? null,
    visibleSort: visibleSort ?? null,
    mediaState: mediaState ?? null,
    savingsBand: savingsBand ?? null,
  }),
  loadRouteContext: async (
    culture: string,
    page: number,
    categorySlug?: string,
    search?: string,
    visibleState?: string,
    visibleSort?: string,
    mediaState?: string,
    savingsBand?: string,
  ) => {
    const routeContext = await getCatalogIndexRouteContext(
      culture,
      page,
      categorySlug,
      search,
    );
    const normalizedVisibleState = readCatalogVisibleState(visibleState);
    const normalizedVisibleSort = readCatalogVisibleSort(visibleSort);
    const normalizedMediaState = readCatalogMediaState(mediaState);
    const normalizedSavingsBand = readCatalogSavingsBand(savingsBand);
    const hasBrowseLens =
      normalizedVisibleState !== "all" ||
      normalizedVisibleSort !== "featured" ||
      normalizedMediaState !== "all" ||
      normalizedSavingsBand !== "all";
    const pageSize =
      routeContext.browseContext.productsResult.data?.request.pageSize ?? 12;
    const matchingSetResult = hasBrowseLens
      ? await getCatalogBrowseSet(culture, categorySlug, search)
      : null;
    const matchingProducts =
      matchingSetResult?.status === "ok" && matchingSetResult.data
        ? matchingSetResult.data.items
        : routeContext.browseContext.productsResult.data?.items ?? [];
    const visibleWindow = hasBrowseLens
      ? buildCatalogVisibleWindow(matchingProducts, {
          page,
          pageSize,
          visibleState: normalizedVisibleState,
          visibleSort: normalizedVisibleSort,
          mediaState: normalizedMediaState,
          savingsBand: normalizedSavingsBand,
        })
      : {
          items: routeContext.browseContext.productsResult.data?.items ?? [],
          total: routeContext.browseContext.productsResult.data?.total ?? 0,
          totalPages: Math.max(
            1,
            Math.ceil(
              (routeContext.browseContext.productsResult.data?.total ?? 0) /
                pageSize,
            ),
          ),
          currentPage: page,
        };

    return {
      ...routeContext,
      visibleWindow,
      matchingSetResult,
      matchingProductsTotal:
        matchingSetResult?.data?.total ??
        routeContext.browseContext.productsResult.data?.total ??
        0,
      pageSize,
      facetSummary: summarizeCatalogFacets(matchingProducts),
      hasBrowseLens,
    };
  },
  getSuccessContext: (result) => ({
    ...summarizeCatalogIndexPageHealth(result),
    catalogIndexContextFootprint:
      summarizeCatalogIndexContextFootprint({
        cmsPagesResult: result.storefrontContext?.cmsPagesResult,
        productsResult: result.storefrontContext?.productsResult,
        cartSummary: {
          status: result.storefrontContext?.storefrontCartStatus ?? "missing",
        },
      }),
  }),
});

const getCachedCatalogDetailPageContext = createPublicDiscoveryPageLoader({
  area: "catalog-page-context",
  operation: "load-detail-page-context",
  thresholdMs: 300,
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
    culture,
    route: "/catalog/[slug]",
    slug,
    categorySlug: category ?? null,
    visibleQuery: visibleQuery ?? null,
    visibleState: visibleState ?? null,
    visibleSort: visibleSort ?? null,
    mediaState: mediaState ?? null,
    savingsBand: savingsBand ?? null,
  }),
  getSuccessContext: summarizeCatalogRouteHealth,
  loadRouteContext: async (
    culture: string,
    slug: string,
    category?: string,
    visibleQuery?: string,
    visibleState?: string,
    visibleSort?: string,
    mediaState?: string,
    savingsBand?: string,
  ) =>
    getCatalogDetailRouteContext(
      culture,
      slug,
      category,
      visibleQuery,
      visibleState,
      visibleSort,
      mediaState,
      savingsBand,
    ),
});

export function getCatalogIndexPageContext(
  culture: string,
  page: number,
  categorySlug?: string,
  search?: string,
  visibleState?: string,
  visibleSort?: string,
  mediaState?: string,
  savingsBand?: string,
) {
  return getCachedCatalogIndexPageContext(
    culture,
    page,
    categorySlug,
    search,
    visibleState,
    visibleSort,
    mediaState,
    savingsBand,
  );
}

export function getCatalogDetailPageContext(
  culture: string,
  slug: string,
  category?: string,
  visibleQuery?: string,
  visibleState?: string,
  visibleSort?: string,
  mediaState?: string,
  savingsBand?: string,
) {
  return getCachedCatalogDetailPageContext(
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

