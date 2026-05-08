import "server-only";
import {
  buildCmsVisibleWindow,
  readCmsMetadataFocus,
  readCmsVisibleSort,
  readCmsVisibleState,
  summarizeCmsMetadataDebt,
} from "@/features/cms/discovery";
import { getCmsBrowseSet } from "@/features/cms/server/get-cms-browse-set";
import {
  getCmsDetailRouteContext,
  getCmsIndexRouteContext,
} from "@/features/cms/server/get-cms-route-context";
import { createPublicDiscoveryPageLoader } from "@/features/storefront/server/create-public-discovery-page-loader";
import {
  summarizeCmsIndexPageHealth,
  summarizeCmsRouteHealth,
} from "@/lib/route-health";

type CmsIndexContextFootprintSource = {
  categoriesResult?: { status: string; data?: { items?: Array<unknown> } | null } | null;
  productsResult?: { status: string; data?: { items?: Array<unknown> } | null } | null;
  cartSummary?: { status: string } | null;
};

export function summarizeCmsIndexContextFootprint(
  result: CmsIndexContextFootprintSource,
) {
  return `categories:${result.categoriesResult?.status ?? "unknown"}:${result.categoriesResult?.data?.items?.length ?? 0}|products:${result.productsResult?.status ?? "unknown"}:${result.productsResult?.data?.items?.length ?? 0}|cart:${result.cartSummary?.status ?? "missing"}`;
}

const getCachedCmsIndexPageContext = createPublicDiscoveryPageLoader({
  area: "cms-page-context",
  operation: "load-index-page-context",
  thresholdMs: 300,
  getContext: (
    culture: string,
    page: number,
    search?: string,
    visibleState?: string,
    visibleSort?: string,
    metadataFocus?: string,
  ) => ({
    culture,
    route: "/cms",
    page,
    search: search ?? null,
    visibleState: visibleState ?? null,
    visibleSort: visibleSort ?? null,
    metadataFocus: metadataFocus ?? null,
  }),
  loadRouteContext: async (
    culture: string,
    page: number,
    search?: string,
    visibleState?: string,
    visibleSort?: string,
    metadataFocus?: string,
  ) => {
    const routeContext = await getCmsIndexRouteContext(culture, page, search);
    const normalizedVisibleState = readCmsVisibleState(visibleState);
    const normalizedVisibleSort = readCmsVisibleSort(visibleSort);
    const normalizedMetadataFocus = readCmsMetadataFocus(metadataFocus);
    const hasBrowseLens =
      normalizedVisibleState !== "all" ||
      normalizedVisibleSort !== "featured" ||
      normalizedMetadataFocus !== "all";
    const pageSize = routeContext.browseContext.pagesResult.data?.request.pageSize ?? 12;
    const matchingSetResult = hasBrowseLens
      ? await getCmsBrowseSet(culture, search)
      : null;
    const matchingPages =
      matchingSetResult?.status === "ok" && matchingSetResult.data
        ? matchingSetResult.data.items
        : routeContext.browseContext.pagesResult.data?.items ?? [];
    const visibleWindow = hasBrowseLens
      ? buildCmsVisibleWindow(matchingPages, {
          page,
          pageSize,
          visibleState: normalizedVisibleState,
          visibleSort: normalizedVisibleSort,
          metadataFocus: normalizedMetadataFocus,
        })
      : {
          items: routeContext.browseContext.pagesResult.data?.items ?? [],
          total: routeContext.browseContext.pagesResult.data?.total ?? 0,
          totalPages: Math.max(
            1,
            Math.ceil(
              (routeContext.browseContext.pagesResult.data?.total ?? 0) / pageSize,
            ),
          ),
          currentPage: page,
        };

    return {
      ...routeContext,
      visibleWindow,
      matchingSetResult,
      matchingItemsTotal:
        matchingSetResult?.data?.total ??
        routeContext.browseContext.pagesResult.data?.total ??
        0,
      pageSize,
      metadataSummary: summarizeCmsMetadataDebt(matchingPages),
      hasBrowseLens,
    };
  },
  getSuccessContext: (result) => ({
    ...summarizeCmsIndexPageHealth(result),
    cmsIndexContextFootprint: summarizeCmsIndexContextFootprint({
      categoriesResult: result.storefrontContext?.categoriesResult,
      productsResult: result.storefrontContext?.productsResult,
      cartSummary: {
        status: result.storefrontContext?.storefrontCartStatus ?? "missing",
      },
    }),
  }),
});

const getCachedCmsDetailPageContext = createPublicDiscoveryPageLoader({
  area: "cms-page-context",
  operation: "load-detail-page-context",
  thresholdMs: 300,
  getContext: (
    culture: string,
    slug: string,
    visibleQuery?: string,
    visibleState?: string,
    visibleSort?: string,
    metadataFocus?: string,
  ) => ({
    culture,
    route: "/page/[slug]",
    slug,
    visibleQuery: visibleQuery ?? null,
    visibleState: visibleState ?? null,
    visibleSort: visibleSort ?? null,
    metadataFocus: metadataFocus ?? null,
  }),
  getSuccessContext: summarizeCmsRouteHealth,
  loadRouteContext: async (
    culture: string,
    slug: string,
    visibleQuery?: string,
    visibleState?: string,
    visibleSort?: string,
    metadataFocus?: string,
  ) =>
    getCmsDetailRouteContext(
      culture,
      slug,
      visibleQuery,
      visibleState,
      visibleSort,
      metadataFocus,
    ),
  sliceOptions: {
    categoryCount: 3,
    productCount: 3,
  },
});

export function getCmsIndexPageContext(
  culture: string,
  page: number,
  search?: string,
  visibleState?: string,
  visibleSort?: string,
  metadataFocus?: string,
) {
  return getCachedCmsIndexPageContext(
    culture,
    page,
    search,
    visibleState,
    visibleSort,
    metadataFocus,
  );
}

export function getCmsDetailPageContext(
  culture: string,
  slug: string,
  visibleQuery?: string,
  visibleState?: string,
  visibleSort?: string,
  metadataFocus?: string,
) {
  return getCachedCmsDetailPageContext(
    culture,
    slug,
    visibleQuery,
    visibleState,
    visibleSort,
    metadataFocus,
  );
}
