import "server-only";
import { cache } from "react";
import {
  getPublicCategories,
  getPublicProducts,
} from "@/features/catalog/api/public-catalog";
import { getPublishedPages } from "@/features/cms/api/public-cms";
import { observeAsyncOperation } from "@/lib/route-observability";

const getCachedStorefrontContinuationContext = cache(async (culture: string) => {
  const [cmsPagesResult, categoriesResult, productsResult] =
    await observeAsyncOperation(
      {
        area: "storefront-continuation",
        operation: "load-context",
        thresholdMs: 250,
      },
      () =>
        Promise.all([
          getPublishedPages({ page: 1, pageSize: 3, culture }),
          getPublicCategories(culture),
          getPublicProducts({ page: 1, pageSize: 3, culture }),
        ]),
    );

  return {
    cmsPages: cmsPagesResult.data?.items ?? [],
    cmsPagesStatus: cmsPagesResult.status,
    categories: categoriesResult.data?.items.slice(0, 3) ?? [],
    categoriesStatus: categoriesResult.status,
    products: productsResult.data?.items ?? [],
    productsStatus: productsResult.status,
  };
});

export async function getStorefrontContinuationContext(culture: string) {
  return getCachedStorefrontContinuationContext(culture);
}
