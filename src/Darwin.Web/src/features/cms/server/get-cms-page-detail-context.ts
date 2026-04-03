import "server-only";
import {
  getPublishedPageSet,
  getPublicPageBySlug,
} from "@/features/cms/api/public-cms";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeCmsDetailCoreHealth } from "@/lib/route-health";
import { cmsDetailObservationContext } from "@/lib/route-observation-context";

const getCachedCmsPageDetailContext = createCachedObservedLoader({
  area: "cms-detail",
  operation: "load-core-context",
  thresholdMs: 250,
  getContext: (culture: string, slug: string) =>
    cmsDetailObservationContext(culture, slug),
  getSuccessContext: summarizeCmsDetailCoreHealth,
  load: async (culture: string, slug: string) => {
    const [pageResult, relatedPagesSeed] = await Promise.all([
      getPublicPageBySlug(slug, culture),
      getPublishedPageSet(culture),
    ]);

    return {
      pageResult,
      relatedPagesResult: relatedPagesSeed,
      relatedPagesSeed,
    };
  },
});

export async function getCmsPageDetailContext(culture: string, slug: string) {
  return getCachedCmsPageDetailContext(culture, slug);
}
