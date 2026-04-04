import "server-only";
import { getPublicProductSet } from "@/features/catalog/api/public-catalog";
import { createCachedObservedLoader } from "@/lib/observed-loader";

const getCachedCatalogBrowseSet = createCachedObservedLoader({
  area: "catalog-browse",
  operation: "load-matching-product-set",
  thresholdMs: 300,
  getContext: (culture: string, categorySlug?: string, search?: string) => ({
    culture,
    categorySlug: categorySlug ?? null,
    search: search ?? null,
  }),
  getSuccessContext: (result) => ({
    status: result.status,
    itemCount: result.data?.items.length ?? 0,
    totalCount: result.data?.total ?? 0,
  }),
  load: async (culture: string, categorySlug?: string, search?: string) =>
    getPublicProductSet({
      culture,
      categorySlug,
      search,
    }),
});

export function getCatalogBrowseSet(
  culture: string,
  categorySlug?: string,
  search?: string,
) {
  return getCachedCatalogBrowseSet(culture, categorySlug, search);
}
