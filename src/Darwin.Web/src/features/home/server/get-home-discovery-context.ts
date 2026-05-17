import "server-only";
import type { PublicCategorySummary } from "@/features/catalog/types";
import { getPublicStorefrontContext } from "@/features/storefront/server/get-public-storefront-context";
import {
  createCachedObservedLoader,
  createObservedLoader,
} from "@/lib/observed-loader";
import {
  summarizeHomeDiscoveryHealth,
  summarizePublicStorefrontHealth,
} from "@/lib/route-health";
import {
  homeDiscoveryObservationContext,
} from "@/lib/route-observation-context";

type HomeDiscoveryStorefrontFootprintSource = {
  storefrontContext: Parameters<typeof summarizePublicStorefrontHealth>[0];
};

export function summarizeHomeDiscoveryStorefrontFootprint(
  result: HomeDiscoveryStorefrontFootprintSource,
) {
  const storefront = result.storefrontContext;

  return `cms:${storefront.cmsPagesStatus}:${storefront.cmsPages.length}|categories:${storefront.categoriesStatus}:${storefront.categories.length}|products:${storefront.productsStatus}:${storefront.products.length}|cart:${storefront.storefrontCartStatus}`;
}

const loadHomeCoreContext = createObservedLoader({
  area: "home-discovery",
  operation: "load-core-context",
  thresholdMs: 250,
  getContext: (culture: string) => homeDiscoveryObservationContext(culture),
  getSuccessContext: summarizePublicStorefrontHealth,
  load: (culture: string) => getPublicStorefrontContext(culture),
});

const getCachedHomeDiscoveryContext = createCachedObservedLoader({
  area: "home-discovery",
  operation: "load-discovery-context",
  thresholdMs: 275,
  getContext: (culture: string) => homeDiscoveryObservationContext(culture),
  getSuccessContext: (result) => ({
    ...summarizeHomeDiscoveryHealth(result),
    homeDiscoveryStorefrontFootprint:
      summarizeHomeDiscoveryStorefrontFootprint(result),
  }),
  load: async (culture: string) => {
    const storefrontContext = await loadHomeCoreContext(culture);
    const { cmsPagesResult: pagesResult, productsResult, categoriesResult } =
      storefrontContext;
    const visibleCategories = (categoriesResult.data?.items ?? []).slice(0, 3);
    const categorySpotlights = buildHomeCategorySpotlights(
      visibleCategories,
      categoriesResult.status,
    );

    return {
      storefrontContext,
      pagesResult,
      productsResult,
      categoriesResult,
      categorySpotlights,
    };
  },
});

export async function getHomeDiscoveryContext(culture: string) {
  return getCachedHomeDiscoveryContext(culture);
}

function buildHomeCategorySpotlights(
  categories: PublicCategorySummary[],
  status: string,
) {
  return categories.map((category) => ({
    category,
    status,
    product: null,
  }));
}
