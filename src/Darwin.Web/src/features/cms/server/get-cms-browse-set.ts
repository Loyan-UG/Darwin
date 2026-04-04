import "server-only";
import { getPublishedPageSet } from "@/features/cms/api/public-cms";
import { createCachedObservedLoader } from "@/lib/observed-loader";

const getCachedCmsBrowseSet = createCachedObservedLoader({
  area: "cms-browse",
  operation: "load-matching-page-set",
  thresholdMs: 300,
  getContext: (culture: string, search?: string) => ({
    culture,
    search: search ?? null,
  }),
  getSuccessContext: (result) => ({
    status: result.status,
    itemCount: result.data?.items.length ?? 0,
    totalCount: result.data?.total ?? 0,
  }),
  load: async (culture: string, search?: string) =>
    getPublishedPageSet({
      culture,
      search,
    }),
});

export function getCmsBrowseSet(culture: string, search?: string) {
  return getCachedCmsBrowseSet(culture, search);
}
