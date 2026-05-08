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

type CmsPageDetailDiscoveryWindow = {
  visibleQuery?: string;
  visibleState: CmsVisibleState;
  visibleSort: CmsVisibleSort;
  metadataFocus: CmsMetadataFocus;
};

type CmsDetailContextFootprintSource = {
  relatedPagesSeed: {
    status: string;
    data?: {
      items?: Array<unknown>;
    } | null;
  };
  relatedPagesResult?: { status: string } | null;
  relatedPages: Array<unknown>;
};

function normalizeDiscoveryWindow(
  discoveryWindow?: Partial<CmsPageDetailDiscoveryWindow>,
): CmsPageDetailDiscoveryWindow {
  return {
    visibleQuery: discoveryWindow?.visibleQuery?.trim() || undefined,
    visibleState: discoveryWindow?.visibleState ?? "all",
    visibleSort: discoveryWindow?.visibleSort ?? "featured",
    metadataFocus: discoveryWindow?.metadataFocus ?? "all",
  };
}

export function summarizeCmsDetailContextFootprint(
  result: CmsDetailContextFootprintSource,
) {
  const seededCount = result.relatedPagesSeed.data?.items?.length ?? 0;

  return `seed:${result.relatedPagesSeed.status}:${seededCount}|visible:${result.relatedPagesResult?.status ?? result.relatedPagesSeed.status}:${result.relatedPages.length}`;
}

const getCachedCmsPageDetailContext = createCachedObservedLoader({
  area: "cms-detail",
  operation: "load-core-context",
  thresholdMs: 250,
  normalizeArgs: (
    culture: string,
    slug: string,
    discoveryWindow?: Partial<CmsPageDetailDiscoveryWindow>,
  ): [string, string, CmsPageDetailDiscoveryWindow] => [
    culture,
    slug,
    normalizeDiscoveryWindow(discoveryWindow),
  ],
  getContext: (
    culture: string,
    slug: string,
    discoveryWindow?: Partial<CmsPageDetailDiscoveryWindow>,
  ) => {
    const normalizedDiscoveryWindow = normalizeDiscoveryWindow(discoveryWindow);

    return {
      ...cmsDetailObservationContext(culture, slug),
      visibleQuery: normalizedDiscoveryWindow.visibleQuery ?? null,
      visibleState:
        normalizedDiscoveryWindow.visibleState !== "all"
          ? normalizedDiscoveryWindow.visibleState
          : null,
      visibleSort:
        normalizedDiscoveryWindow.visibleSort !== "featured"
          ? normalizedDiscoveryWindow.visibleSort
          : null,
      metadataFocus:
        normalizedDiscoveryWindow.metadataFocus !== "all"
          ? normalizedDiscoveryWindow.metadataFocus
          : null,
    };
  },
  getSuccessContext: (result) => ({
    ...summarizeCmsDetailCoreHealth(result),
    cmsDetailContextFootprint:
      summarizeCmsDetailContextFootprint(result),
  }),
  load: async (
    culture: string,
    slug: string,
    discoveryWindow?: Partial<CmsPageDetailDiscoveryWindow>,
  ) => {
    const normalizedDiscoveryWindow = normalizeDiscoveryWindow(discoveryWindow);
    const [pageResult, relatedPagesSeed] = await Promise.all([
      getPublicPageBySlug(slug, culture),
      getPublishedPageSet({
        culture,
        search: normalizedDiscoveryWindow.visibleQuery,
      }),
    ]);

    const relatedPages =
      relatedPagesSeed.data?.items && relatedPagesSeed.status === "ok"
        ? sortVisiblePages(
            filterVisiblePages(
              relatedPagesSeed.data.items,
              normalizedDiscoveryWindow.visibleState,
              undefined,
              normalizedDiscoveryWindow.metadataFocus,
            ),
            normalizedDiscoveryWindow.visibleSort,
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
  discoveryWindow?: Partial<CmsPageDetailDiscoveryWindow>,
) {
  return getCachedCmsPageDetailContext(
    culture,
    slug,
    normalizeDiscoveryWindow(discoveryWindow),
  );
}

