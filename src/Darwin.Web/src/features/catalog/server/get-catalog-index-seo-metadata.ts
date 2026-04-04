import "server-only";
import {
  readPositiveIntegerSearchParam,
  readSearchTextParam,
} from "@/features/checkout/helpers";
import {
  readCatalogMediaState,
  readCatalogSavingsBand,
  readCatalogVisibleSort,
  readCatalogVisibleState,
} from "@/features/catalog/discovery";
import { buildAppQueryPath } from "@/lib/locale-routing";
import { catalogIndexRouteObservationContext } from "@/lib/route-observation-context";
import { buildSeoMetadata } from "@/lib/seo";
import { createCachedObservedSeoMetadataLoader } from "@/lib/seo-loader";
import { getCatalogResource } from "@/localization";

type CatalogSeoSearchParams = {
  category?: string;
  page?: string;
  search?: string;
  visibleQuery?: string;
  visibleState?: string;
  visibleSort?: string;
  mediaState?: string;
  savingsBand?: string;
};

function buildCatalogPath(
  category?: string,
  page?: number,
  search?: string,
  visibleQuery?: string,
  visibleState?: string,
  visibleSort?: string,
  mediaState?: string,
  savingsBand?: string,
) {
  return buildAppQueryPath("/catalog", {
    category,
    page: page && page > 1 ? page : undefined,
    search,
    visibleQuery,
    visibleState: visibleState && visibleState !== "all" ? visibleState : undefined,
    visibleSort: visibleSort && visibleSort !== "featured" ? visibleSort : undefined,
    mediaState: mediaState && mediaState !== "all" ? mediaState : undefined,
    savingsBand: savingsBand && savingsBand !== "all" ? savingsBand : undefined,
  });
}

export const getCatalogIndexSeoMetadata = createCachedObservedSeoMetadataLoader({
  area: "catalog-seo",
  operation: "load-index-seo-metadata",
  thresholdMs: 175,
  getContext: (culture: string, searchParams?: CatalogSeoSearchParams) =>
    catalogIndexRouteObservationContext(
      culture,
      readPositiveIntegerSearchParam(searchParams?.page),
      readSearchTextParam(searchParams?.category, 80),
    ),
  load: async (culture: string, searchParams?: CatalogSeoSearchParams) => {
    const copy = getCatalogResource(culture);
    const category = readSearchTextParam(searchParams?.category, 80);
    const search = readSearchTextParam(searchParams?.search, 80);
    const visibleQuery = readSearchTextParam(searchParams?.visibleQuery, 80);
    const visibleState = readCatalogVisibleState(searchParams?.visibleState);
    const visibleSort = readCatalogVisibleSort(searchParams?.visibleSort);
    const mediaState = readCatalogMediaState(searchParams?.mediaState);
    const savingsBand = readCatalogSavingsBand(searchParams?.savingsBand);
    const safePage = readPositiveIntegerSearchParam(searchParams?.page);
    const canonicalPath = buildCatalogPath(
      category,
      safePage,
      search,
      visibleQuery,
      visibleState,
      visibleSort,
      mediaState,
      savingsBand,
    );
    const noIndex =
      Boolean(category) ||
      safePage > 1 ||
      Boolean(search) ||
      Boolean(visibleQuery) ||
      visibleState !== "all" ||
      visibleSort !== "featured" ||
      mediaState !== "all" ||
      savingsBand !== "all";

    return {
      metadata: buildSeoMetadata({
        culture,
        title: copy.catalogMetaTitle,
        description: copy.catalogMetaDescription,
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
