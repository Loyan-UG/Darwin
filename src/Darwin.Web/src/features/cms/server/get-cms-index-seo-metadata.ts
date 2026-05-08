import "server-only";
import {
  readCmsMetadataFocus,
  readCmsVisibleSort,
  readCmsVisibleState,
} from "@/features/cms/discovery";
import { buildAppQueryPath } from "@/lib/locale-routing";
import { cmsIndexRouteObservationContext } from "@/lib/route-observation-context";
import { buildSeoMetadata, buildStablePublicLanguageAlternates } from "@/lib/seo";
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

function normalizeCmsIndexSeoArgs(
  culture: string,
  page = 1,
  search?: string,
  visibleQuery?: string,
  visibleState?: string,
  visibleSort?: string,
  metadataFocus?: string,
): CmsIndexSeoArgs {
  return [
    culture.trim(),
    Number.isFinite(page) && page > 0 ? Math.floor(page) : 1,
    search?.trim() || undefined,
    visibleQuery?.trim() || undefined,
    readCmsVisibleState(visibleState),
    readCmsVisibleSort(visibleSort),
    readCmsMetadataFocus(metadataFocus),
  ];
}

export const getCmsIndexSeoMetadata =
  createCachedObservedSeoMetadataLoader<CmsIndexSeoArgs>({
  area: "cms-seo",
  operation: "load-index-seo-metadata",
  thresholdMs: 175,
  normalizeArgs: normalizeCmsIndexSeoArgs,
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
    const normalizedVisibleState = visibleState ?? "all";
    const normalizedVisibleSort = visibleSort ?? "featured";
    const normalizedMetadataFocus = metadataFocus ?? "all";
    const canonicalPath = buildAppQueryPath("/help", {
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
    const languageAlternates = !noIndex
      ? buildStablePublicLanguageAlternates(canonicalPath)
      : undefined;

    return {
      metadata: buildSeoMetadata({
        culture,
        title: shared.cmsIndexMetaTitle,
        description: shared.cmsIndexMetaDescription,
        path: canonicalPath,
        noIndex,
        languageAlternates,
      }),
      canonicalPath,
      noIndex,
      languageAlternates,
    };
  },
});
