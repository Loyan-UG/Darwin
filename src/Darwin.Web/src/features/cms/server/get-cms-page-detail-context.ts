import "server-only";
import {
  getPublishedPageSet,
  getPublicPageBySlug,
} from "@/features/cms/api/public-cms";
import {
  filterVisiblePages,
  sortVisiblePages,
  type CmsMetadataFocus,
  type CmsVisibleSort,
  type CmsVisibleState,
} from "@/features/cms/discovery";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeCmsDetailCoreHealth } from "@/lib/route-health";
import { cmsDetailObservationContext } from "@/lib/route-observation-context";

type CmsPageDetailReviewWindow = {
  visibleQuery?: string;
  visibleState: CmsVisibleState;
  visibleSort: CmsVisibleSort;
  metadataFocus: CmsMetadataFocus;
};

function normalizeReviewWindow(
  reviewWindow?: Partial<CmsPageDetailReviewWindow>,
): CmsPageDetailReviewWindow {
  return {
    visibleQuery: reviewWindow?.visibleQuery?.trim() || undefined,
    visibleState: reviewWindow?.visibleState ?? "all",
    visibleSort: reviewWindow?.visibleSort ?? "featured",
    metadataFocus: reviewWindow?.metadataFocus ?? "all",
  };
}

const getCachedCmsPageDetailContext = createCachedObservedLoader({
  area: "cms-detail",
  operation: "load-core-context",
  thresholdMs: 250,
  getContext: (
    culture: string,
    slug: string,
    reviewWindow?: Partial<CmsPageDetailReviewWindow>,
  ) => {
    const normalizedReviewWindow = normalizeReviewWindow(reviewWindow);

    return {
      ...cmsDetailObservationContext(culture, slug),
      visibleQuery: normalizedReviewWindow.visibleQuery ?? null,
      visibleState:
        normalizedReviewWindow.visibleState !== "all"
          ? normalizedReviewWindow.visibleState
          : null,
      visibleSort:
        normalizedReviewWindow.visibleSort !== "featured"
          ? normalizedReviewWindow.visibleSort
          : null,
      metadataFocus:
        normalizedReviewWindow.metadataFocus !== "all"
          ? normalizedReviewWindow.metadataFocus
          : null,
    };
  },
  getSuccessContext: summarizeCmsDetailCoreHealth,
  load: async (
    culture: string,
    slug: string,
    reviewWindow?: Partial<CmsPageDetailReviewWindow>,
  ) => {
    const normalizedReviewWindow = normalizeReviewWindow(reviewWindow);
    const [pageResult, relatedPagesSeed] = await Promise.all([
      getPublicPageBySlug(slug, culture),
      getPublishedPageSet({
        culture,
        search: normalizedReviewWindow.visibleQuery,
      }),
    ]);

    const relatedPages =
      relatedPagesSeed.data?.items && relatedPagesSeed.status === "ok"
        ? sortVisiblePages(
            filterVisiblePages(
              relatedPagesSeed.data.items,
              normalizedReviewWindow.visibleState,
              undefined,
              normalizedReviewWindow.metadataFocus,
            ),
            normalizedReviewWindow.visibleSort,
          )
        : relatedPagesSeed.data?.items ?? [];

    return {
      pageResult,
      relatedPagesResult: relatedPagesSeed,
      relatedPagesSeed,
      relatedPages,
    };
  },
});

export async function getCmsPageDetailContext(
  culture: string,
  slug: string,
  reviewWindow?: Partial<CmsPageDetailReviewWindow>,
) {
  return getCachedCmsPageDetailContext(culture, slug, reviewWindow);
}
