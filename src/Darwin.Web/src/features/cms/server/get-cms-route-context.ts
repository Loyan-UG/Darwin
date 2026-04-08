import "server-only";
import { getCmsBrowseContext } from "@/features/cms/server/get-cms-browse-context";
import { getCmsPageDetailContext } from "@/features/cms/server/get-cms-page-detail-context";
import { getPublicStorefrontContext } from "@/features/storefront/server/get-public-storefront-context";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeCmsRouteHealth } from "@/lib/route-health";
import {
  readCmsMetadataFocus,
  readCmsVisibleSort,
  readCmsVisibleState,
} from "@/features/cms/discovery";
import {
  cmsDetailRouteObservationContext,
  cmsIndexRouteObservationContext,
} from "@/lib/route-observation-context";

function normalizeCmsIndexArgs(
  culture: string,
  page: number,
  search?: string,
): [string, number, string | undefined] {
  return [
    culture,
    Number.isFinite(page) && page > 0 ? Math.floor(page) : 1,
    search?.trim() || undefined,
  ];
}

function normalizeCmsDetailArgs(
  culture: string,
  slug: string,
  visibleQuery?: string,
  visibleState?: string,
  visibleSort?: string,
  metadataFocus?: string,
): [
  string,
  string,
  string | undefined,
  string | undefined,
  string | undefined,
  string | undefined,
] {
  return [
    culture,
    slug,
    visibleQuery?.trim() || undefined,
    readCmsVisibleState(visibleState),
    readCmsVisibleSort(visibleSort),
    readCmsMetadataFocus(metadataFocus),
  ];
}

const getCachedCmsIndexRouteContext = createCachedObservedLoader({
  area: "cms-index",
  operation: "load-route-context",
  thresholdMs: 325,
  normalizeArgs: normalizeCmsIndexArgs,
  getContext: (culture: string, page: number, search?: string) =>
    cmsIndexRouteObservationContext(culture, page, search),
  getSuccessContext: summarizeCmsRouteHealth,
  load: async (culture: string, page: number, search?: string) => {
    const [browseContext, storefrontContext] = await Promise.all([
      getCmsBrowseContext(culture, page, search),
      getPublicStorefrontContext(culture),
    ]);

    return {
      browseContext,
      storefrontContext,
    };
  },
});

const getCachedCmsDetailRouteContext = createCachedObservedLoader({
  area: "cms-detail",
  operation: "load-route-context",
  thresholdMs: 325,
  normalizeArgs: normalizeCmsDetailArgs,
  getContext: (
    culture: string,
    slug: string,
    visibleQuery?: string,
    visibleState?: string,
    visibleSort?: string,
    metadataFocus?: string,
  ) => ({
    ...cmsDetailRouteObservationContext(culture, slug),
    visibleQuery: visibleQuery ?? null,
    visibleState: visibleState ?? null,
    visibleSort: visibleSort ?? null,
    metadataFocus: metadataFocus ?? null,
  }),
  getSuccessContext: summarizeCmsRouteHealth,
  load: async (
    culture: string,
    slug: string,
    visibleQuery?: string,
    visibleState?: string,
    visibleSort?: string,
    metadataFocus?: string,
  ) => {
    const [detailContext, storefrontContext] = await Promise.all([
      getCmsPageDetailContext(culture, slug, {
        visibleQuery,
        visibleState:
          visibleState === "ready" || visibleState === "needs-attention"
            ? visibleState
            : "all",
        visibleSort:
          visibleSort === "title-asc" ||
          visibleSort === "ready-first" ||
          visibleSort === "attention-first"
            ? visibleSort
            : "featured",
        metadataFocus:
          metadataFocus === "missing-title" ||
          metadataFocus === "missing-description" ||
          metadataFocus === "missing-both"
            ? metadataFocus
            : "all",
      }),
      getPublicStorefrontContext(culture),
    ]);

    return {
      detailContext,
      storefrontContext,
    };
  },
});

export async function getCmsIndexRouteContext(
  culture: string,
  page: number,
  search?: string,
) {
  return getCachedCmsIndexRouteContext(culture, page, search);
}

export async function getCmsDetailRouteContext(
  culture: string,
  slug: string,
  visibleQuery?: string,
  visibleState?: string,
  visibleSort?: string,
  metadataFocus?: string,
) {
  return getCachedCmsDetailRouteContext(
    culture,
    slug,
    visibleQuery,
    visibleState,
    visibleSort,
    metadataFocus,
  );
}
