import "server-only";
import {
  readCmsMetadataFocus,
  readCmsVisibleSort,
  readCmsVisibleState,
} from "@/features/cms/discovery";
import { buildAppQueryPath } from "@/lib/locale-routing";
import { cmsIndexRouteObservationContext } from "@/lib/route-observation-context";
import { buildSeoMetadata } from "@/lib/seo";
import { createCachedObservedSeoMetadataLoader } from "@/lib/seo-loader";
import { getSharedResource } from "@/localization";

type CmsIndexSeoArgs = [
  culture: string,
  page?: number,
  search?: string,
  visibleQuery?: string,
  visibleState?: string,
  visibleSort?: string,
  metadataFocus?: string,
];

export const getCmsIndexSeoMetadata =
  createCachedObservedSeoMetadataLoader<CmsIndexSeoArgs>({
  area: "cms-seo",
  operation: "load-index-seo-metadata",
  thresholdMs: 175,
  getContext: (
    culture: string,
    page = 1,
    search?: string,
    _visibleQuery?: string,
    _visibleState?: string,
    _visibleSort?: string,
    _metadataFocus?: string,
  ) => {
    void _visibleQuery;
    void _visibleState;
    void _visibleSort;
    void _metadataFocus;

    return cmsIndexRouteObservationContext(culture, page, search);
  },
  load: async (
    culture: string,
    page = 1,
    search?: string,
    visibleQuery?: string,
    visibleState?: string,
    visibleSort?: string,
    metadataFocus?: string,
  ) => {
    const shared = getSharedResource(culture);
    const normalizedVisibleState = readCmsVisibleState(visibleState);
    const normalizedVisibleSort = readCmsVisibleSort(visibleSort);
    const normalizedMetadataFocus = readCmsMetadataFocus(metadataFocus);
    const canonicalPath = buildAppQueryPath("/cms", {
      page: page > 1 ? page : undefined,
      search,
      visibleQuery,
      visibleState:
        normalizedVisibleState !== "all" ? normalizedVisibleState : undefined,
      visibleSort:
        normalizedVisibleSort !== "featured" ? normalizedVisibleSort : undefined,
      metadataFocus:
        normalizedMetadataFocus !== "all" ? normalizedMetadataFocus : undefined,
    });
    const noIndex =
      page > 1 ||
      Boolean(search) ||
      Boolean(visibleQuery) ||
      normalizedVisibleState !== "all" ||
      normalizedVisibleSort !== "featured" ||
      normalizedMetadataFocus !== "all";

    return {
      metadata: buildSeoMetadata({
        culture,
        title: shared.cmsIndexMetaTitle,
        description: shared.cmsIndexMetaDescription,
        path: canonicalPath,
        noIndex,
        allowLanguageAlternates: !noIndex,
      }),
      canonicalPath,
      noIndex,
      languageAlternates: !noIndex ? {} : undefined,
    };
  },
});
