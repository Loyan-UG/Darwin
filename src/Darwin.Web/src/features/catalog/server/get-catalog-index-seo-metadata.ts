import "server-only";
import {
  readCatalogMediaState,
  readCatalogSavingsBand,
  readCatalogVisibleSort,
  readCatalogVisibleState,
} from "@/features/catalog/discovery";
import { getPublicCategories } from "@/features/catalog/api/public-catalog";
import { buildAppQueryPath } from "@/lib/locale-routing";
import { catalogIndexRouteObservationContext } from "@/lib/route-observation-context";
import { buildSeoMetadata, buildStablePublicLanguageAlternates } from "@/lib/seo";
import { createCachedObservedSeoMetadataLoader } from "@/lib/seo-loader";
import { getCatalogResource } from "@/localization";

type CatalogIndexSeoArgs = [
  culture: string,
  page?: number,
  category?: string,
  search?: string,
  visibleQuery?: string,
  visibleState?: string,
  visibleSort?: string,
  mediaState?: string,
  savingsBand?: string,
];

function normalizeCatalogIndexSeoArgs(
  culture: string,
  page = 1,
  category?: string,
  search?: string,
  visibleQuery?: string,
  visibleState?: string,
  visibleSort?: string,
  mediaState?: string,
  savingsBand?: string,
): CatalogIndexSeoArgs {
  return [
    culture.trim(),
    Number.isFinite(page) && page > 0 ? Math.floor(page) : 1,
    category?.trim() || undefined,
    search?.trim() || undefined,
    visibleQuery?.trim() || undefined,
    readCatalogVisibleState(visibleState),
    readCatalogVisibleSort(visibleSort),
    readCatalogMediaState(mediaState),
    readCatalogSavingsBand(savingsBand),
  ];
}

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

async function getCategorySeoCopy(culture: string, category?: string) {
  const slug = category?.trim();
  if (!slug) {
    return null;
  }

  const categoriesResult = await getPublicCategories(culture);
  if (categoriesResult.status !== "ok" || !categoriesResult.data) {
    return null;
  }

  const activeCategory = categoriesResult.data.items.find(
    (item) => item.slug === slug,
  );
  if (!activeCategory) {
    return null;
  }

  return {
    title: activeCategory.metaTitle?.trim() || activeCategory.name,
    description:
      activeCategory.metaDescription?.trim() ||
      activeCategory.description?.trim() ||
      null,
  };
}

export const getCatalogIndexSeoMetadata =
  createCachedObservedSeoMetadataLoader<CatalogIndexSeoArgs>({
  area: "catalog-seo",
  operation: "load-index-seo-metadata",
  thresholdMs: 175,
  normalizeArgs: normalizeCatalogIndexSeoArgs,
  getContext: (
    culture: string,
    page = 1,
    category?: string,
    search?: string,
    _visibleQuery?: string,
    _visibleState?: string,
    _visibleSort?: string,
    _mediaState?: string,
    _savingsBand?: string,
  ) => {
    void _visibleQuery;
    void _visibleState;
    void _visibleSort;
    void _mediaState;
    void _savingsBand;

    return catalogIndexRouteObservationContext(culture, page, category, search);
  },
  load: async (
    culture: string,
    page = 1,
    category?: string,
    search?: string,
    visibleQuery?: string,
    visibleState?: string,
    visibleSort?: string,
    mediaState?: string,
    savingsBand?: string,
  ) => {
    const copy = getCatalogResource(culture);
    const normalizedVisibleState = visibleState ?? "all";
    const normalizedVisibleSort = visibleSort ?? "featured";
    const normalizedMediaState = mediaState ?? "all";
    const normalizedSavingsBand = savingsBand ?? "all";
    const canonicalPath = buildCatalogPath(
      category,
      page,
      search,
      visibleQuery,
      normalizedVisibleState,
      normalizedVisibleSort,
      normalizedMediaState,
      normalizedSavingsBand,
    );
    const noIndex =
      page > 1 ||
      Boolean(search) ||
      Boolean(visibleQuery) ||
      normalizedVisibleState !== "all" ||
      normalizedVisibleSort !== "featured" ||
      normalizedMediaState !== "all" ||
      normalizedSavingsBand !== "all";
    const languageAlternates = !noIndex
      ? buildStablePublicLanguageAlternates(canonicalPath)
      : undefined;
    const categorySeoCopy = await getCategorySeoCopy(culture, category);

    return {
      metadata: buildSeoMetadata({
        culture,
        title: categorySeoCopy?.title ?? copy.catalogMetaTitle,
        description: categorySeoCopy?.description ?? copy.catalogMetaDescription,
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
