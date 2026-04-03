import "server-only";
import { getCmsBrowseContext } from "@/features/cms/server/get-cms-browse-context";
import { getCmsPageDetailContext } from "@/features/cms/server/get-cms-page-detail-context";
import { getPublicStorefrontContext } from "@/features/storefront/server/get-public-storefront-context";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeCmsRouteHealth } from "@/lib/route-health";
import {
  cmsDetailRouteObservationContext,
  cmsIndexRouteObservationContext,
} from "@/lib/route-observation-context";

const getCachedCmsIndexRouteContext = createCachedObservedLoader({
  area: "cms-index",
  operation: "load-route-context",
  thresholdMs: 325,
  getContext: (culture: string, page: number) =>
    cmsIndexRouteObservationContext(culture, page),
  getSuccessContext: summarizeCmsRouteHealth,
  load: async (culture: string, page: number) => {
    const [browseContext, storefrontContext] = await Promise.all([
      getCmsBrowseContext(culture, page),
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
  getContext: (culture: string, slug: string) =>
    cmsDetailRouteObservationContext(culture, slug),
  getSuccessContext: summarizeCmsRouteHealth,
  load: async (culture: string, slug: string) => {
    const [detailContext, storefrontContext] = await Promise.all([
      getCmsPageDetailContext(culture, slug),
      getPublicStorefrontContext(culture),
    ]);

    return {
      detailContext,
      storefrontContext,
    };
  },
});

export async function getCmsIndexRouteContext(culture: string, page: number) {
  return getCachedCmsIndexRouteContext(culture, page);
}

export async function getCmsDetailRouteContext(culture: string, slug: string) {
  return getCachedCmsDetailRouteContext(culture, slug);
}
