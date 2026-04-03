import "server-only";
import { getPublicProducts } from "@/features/catalog/api/public-catalog";
import { getPublicStorefrontContext } from "@/features/storefront/server/get-public-storefront-context";
import {
  createCachedObservedLoader,
  createObservedLoader,
} from "@/lib/observed-loader";
import {
  summarizeHomeCategorySpotlightsHealth,
  summarizeHomeDiscoveryHealth,
  summarizePublicStorefrontHealth,
} from "@/lib/route-health";
import {
  homeCategorySpotlightsObservationContext,
  homeDiscoveryObservationContext,
} from "@/lib/route-observation-context";

const loadHomeCoreContext = createObservedLoader({
  area: "home-discovery",
  operation: "load-core-context",
  thresholdMs: 250,
  getContext: (culture: string) => homeDiscoveryObservationContext(culture),
  getSuccessContext: summarizePublicStorefrontHealth,
  load: (culture: string) => getPublicStorefrontContext(culture),
});

const loadHomeCategorySpotlights = createObservedLoader({
  area: "home-discovery",
  operation: "load-category-spotlights",
  thresholdMs: 250,
  getContext: (...args: [string, number, string[]]) => {
    const [culture, categoryCount] = args;
    return homeCategorySpotlightsObservationContext(culture, categoryCount);
  },
  getSuccessContext: summarizeHomeCategorySpotlightsHealth,
  load: (culture: string, _categoryCount: number, categorySlugs: string[]) =>
    Promise.all(
      categorySlugs.slice(0, 3).map(async (categorySlug) => {
        const categoryProductsResult = await getPublicProducts({
          page: 1,
          pageSize: 1,
          culture,
          categorySlug,
        });

        return {
          categorySlug,
          categoryProductsResult,
        };
      }),
    ),
});

const getCachedHomeDiscoveryContext = createCachedObservedLoader({
  area: "home-discovery",
  operation: "load-discovery-context",
  thresholdMs: 275,
  getContext: (culture: string) => homeDiscoveryObservationContext(culture),
  getSuccessContext: summarizeHomeDiscoveryHealth,
  load: async (culture: string) => {
    const storefrontContext = await loadHomeCoreContext(culture);
    const { cmsPagesResult: pagesResult, productsResult, categoriesResult } =
      storefrontContext;
    const visibleCategories = (categoriesResult.data?.items ?? []).slice(0, 3);
    const spotlightResults = await loadHomeCategorySpotlights(
      culture,
      categoriesResult.data?.items.length ?? 0,
      visibleCategories.map((category) => category.slug),
    );
    const categorySpotlights = visibleCategories.map((category, index) => ({
      category,
      status: spotlightResults[index]?.categoryProductsResult.status ?? "not-found",
      product:
        spotlightResults[index]?.categoryProductsResult.data?.items[0] ?? null,
    }));

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
