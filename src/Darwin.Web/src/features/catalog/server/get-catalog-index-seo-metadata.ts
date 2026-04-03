import "server-only";
import {
  readPositiveIntegerSearchParam,
  readSearchTextParam,
} from "@/features/checkout/helpers";
import {
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
  visibleQuery?: string;
  visibleState?: string;
  visibleSort?: string;
};

function buildCatalogPath(
  category?: string,
  page?: number,
  visibleQuery?: string,
  visibleState?: string,
  visibleSort?: string,
) {
  return buildAppQueryPath("/catalog", {
    category,
    page: page && page > 1 ? page : undefined,
    visibleQuery,
    visibleState: visibleState && visibleState !== "all" ? visibleState : undefined,
    visibleSort: visibleSort && visibleSort !== "featured" ? visibleSort : undefined,
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
    const visibleQuery = readSearchTextParam(searchParams?.visibleQuery, 80);
    const visibleState = readCatalogVisibleState(searchParams?.visibleState);
    const visibleSort = readCatalogVisibleSort(searchParams?.visibleSort);
    const safePage = readPositiveIntegerSearchParam(searchParams?.page);
    const canonicalPath = buildCatalogPath(
      category,
      safePage,
      visibleQuery,
      visibleState,
      visibleSort,
    );
    const noIndex =
      Boolean(category) ||
      safePage > 1 ||
      Boolean(visibleQuery) ||
      visibleState !== "all" ||
      visibleSort !== "featured";

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
