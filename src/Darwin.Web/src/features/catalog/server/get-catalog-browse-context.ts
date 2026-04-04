import "server-only";
import {
  getPublicCategories,
  getPublicProducts,
} from "@/features/catalog/api/public-catalog";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeCatalogBrowseCoreHealth } from "@/lib/route-health";
import { catalogBrowseObservationContext } from "@/lib/route-observation-context";

const getCachedCatalogBrowseContext = createCachedObservedLoader({
  area: "catalog-browse",
  operation: "load-core-context",
  thresholdMs: 250,
  getContext: (
    culture: string,
    page: number,
    categorySlug?: string,
    search?: string,
  ) => catalogBrowseObservationContext(culture, page, categorySlug, search),
  getSuccessContext: summarizeCatalogBrowseCoreHealth,
  load: async (
    culture: string,
    page: number,
    categorySlug?: string,
    search?: string,
  ) => {
    const [categoriesResult, productsResult] = await Promise.all([
      getPublicCategories(culture),
      getPublicProducts({
        page,
        pageSize: 12,
        culture,
        categorySlug,
        search,
      }),
    ]);

    return {
      categoriesResult,
      productsResult,
    };
  },
});

export async function getCatalogBrowseContext(
  culture: string,
  page: number,
  categorySlug?: string,
  search?: string,
) {
  return getCachedCatalogBrowseContext(culture, page, categorySlug, search);
}
