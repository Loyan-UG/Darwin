import "server-only";
import {
  getPublicCategories,
  getPublicProducts,
} from "@/features/catalog/api/public-catalog";
import { getPublishedPages } from "@/features/cms/api/public-cms";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeStorefrontContinuationHealth } from "@/lib/route-health";
import { storefrontContinuationObservationContext } from "@/lib/route-observation-context";

const getCachedStorefrontContinuationContext = createCachedObservedLoader({
  area: "storefront-continuation",
  operation: "load-context",
  thresholdMs: 250,
  getContext: (culture: string) => storefrontContinuationObservationContext(culture),
  getSuccessContext: summarizeStorefrontContinuationHealth,
  load: async (culture: string) => {
    const [cmsPagesResult, categoriesResult, productsResult] = await Promise.all([
      getPublishedPages({ page: 1, pageSize: 3, culture }),
      getPublicCategories(culture),
      getPublicProducts({ page: 1, pageSize: 3, culture }),
    ]);

    return {
      cmsPagesResult,
      cmsPages: cmsPagesResult.data?.items ?? [],
      cmsPagesStatus: cmsPagesResult.status,
      categoriesResult,
      categories: categoriesResult.data?.items.slice(0, 3) ?? [],
      categoriesStatus: categoriesResult.status,
      productsResult,
      products: productsResult.data?.items ?? [],
      productsStatus: productsResult.status,
    };
  },
});

export async function getStorefrontContinuationContext(culture: string) {
  return getCachedStorefrontContinuationContext(culture);
}
