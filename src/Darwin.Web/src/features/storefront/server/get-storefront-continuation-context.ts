import "server-only";
import {
  getPublicCategories,
  getPublicProducts,
} from "@/features/catalog/api/public-catalog";
import { getPublishedPages } from "@/features/cms/api/public-cms";
import { normalizeCultureArg } from "@/lib/route-context-normalization";
import {
  buildStorefrontContinuationFootprint,
} from "@/lib/shared-context-diagnostics";
import { createSharedContextLoader } from "@/lib/shared-context-loader";
import { summarizeStorefrontContinuationHealth } from "@/lib/route-health";
import { storefrontContinuationObservationContext } from "@/lib/route-observation-context";

export function buildStorefrontContinuationSuccessContext(
  result: {
    cmsPagesStatus: string;
    cmsPages: unknown[];
    categoriesStatus: string;
    categories: unknown[];
    productsStatus: string;
    products: Array<{
      id: string;
      slug: string;
      name: string;
      priceMinor: number;
      currency: string;
      shortDescription?: string | null;
      compareAtPriceMinor?: number | null;
      primaryImageUrl?: string | null;
    }>;
  },
) {
  const summary = summarizeStorefrontContinuationHealth(result);

  return {
    ...summary,
    sharedContextFootprint: buildStorefrontContinuationFootprint({
      cmsStatus: summary.cmsStatus,
      categoriesStatus: summary.categoriesStatus,
      productsStatus: summary.productsStatus,
    }),
  };
}

const getCachedStorefrontContinuationContext = createSharedContextLoader({
  kind: "storefront-continuation",
  area: "storefront-continuation",
  operation: "load-context",
  normalizeArgs: normalizeCultureArg,
  getContext: (culture: string) => storefrontContinuationObservationContext(culture),
  getSuccessContext: (result) => buildStorefrontContinuationSuccessContext(result),
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
