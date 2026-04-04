import "server-only";
import { getPublishedPages } from "@/features/cms/api/public-cms";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeCmsBrowseCoreHealth } from "@/lib/route-health";
import { cmsBrowseObservationContext } from "@/lib/route-observation-context";

const getCachedCmsBrowseContext = createCachedObservedLoader({
  area: "cms-browse",
  operation: "load-core-context",
  thresholdMs: 250,
  getContext: (culture: string, page: number, search?: string) =>
    cmsBrowseObservationContext(culture, page, search),
  getSuccessContext: summarizeCmsBrowseCoreHealth,
  load: async (culture: string, page: number, search?: string) => {
    const pagesResult = await getPublishedPages({
      page,
      pageSize: 12,
      culture,
      search,
    });

    return {
      pagesResult,
    };
  },
});

export async function getCmsBrowseContext(
  culture: string,
  page: number,
  search?: string,
) {
  return getCachedCmsBrowseContext(culture, page, search);
}
