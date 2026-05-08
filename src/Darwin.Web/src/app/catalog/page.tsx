import { CatalogPage } from "@/components/catalog/catalog-page";
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
import { getCatalogIndexPageContext } from "@/features/catalog/server/get-catalog-page-context";
import { getCatalogIndexSeoMetadata } from "@/features/catalog/server/get-catalog-index-seo-metadata";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata({
  searchParams,
}: {
  searchParams?: Promise<{
      category?: string;
      page?: string;
      search?: string;
      visibleQuery?: string;
      visibleState?: string;
      visibleSort?: string;
      mediaState?: string;
      savingsBand?: string;
    }>;
}) {
  const culture = await getRequestCulture();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const safePage = readPositiveIntegerSearchParam(resolvedSearchParams?.page);
  const activeCategorySlug = readSearchTextParam(
    resolvedSearchParams?.category,
    80,
  );
  const search = readSearchTextParam(resolvedSearchParams?.search, 80);
  const visibleQuery = readSearchTextParam(
    resolvedSearchParams?.visibleQuery,
    80,
  );
  const visibleState = readCatalogVisibleState(
    resolvedSearchParams?.visibleState,
  );
  const visibleSort = readCatalogVisibleSort(resolvedSearchParams?.visibleSort);
  const mediaState = readCatalogMediaState(resolvedSearchParams?.mediaState);
  const savingsBand = readCatalogSavingsBand(
    resolvedSearchParams?.savingsBand,
  );
  const { metadata } = await getCatalogIndexSeoMetadata(
    culture,
    safePage,
    activeCategorySlug,
    search,
    visibleQuery,
    visibleState,
    visibleSort,
    mediaState,
    savingsBand,
  );
  return metadata;
}

type CatalogRouteProps = {
  searchParams?: Promise<{
    category?: string;
    page?: string;
    search?: string;
    visibleQuery?: string;
    visibleState?: string;
    visibleSort?: string;
    mediaState?: string;
    savingsBand?: string;
  }>;
};

export default async function CatalogRoute({ searchParams }: CatalogRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const culture = await getRequestCulture();
  const safePage = readPositiveIntegerSearchParam(resolvedSearchParams?.page);
  const activeCategorySlug = readSearchTextParam(
    resolvedSearchParams?.category,
    80,
  );
  const searchQuery = readSearchTextParam(
    resolvedSearchParams?.search ?? resolvedSearchParams?.visibleQuery,
    80,
  );
  const visibleState = readCatalogVisibleState(
    resolvedSearchParams?.visibleState,
  );
  const visibleSort = readCatalogVisibleSort(resolvedSearchParams?.visibleSort);
  const mediaState = readCatalogMediaState(resolvedSearchParams?.mediaState);
  const savingsBand = readCatalogSavingsBand(
    resolvedSearchParams?.savingsBand,
  );

  const {
    browseContext,
    visibleWindow,
    pageSize,
    matchingSetResult,
  } = await getCatalogIndexPageContext(
    culture,
    safePage,
    activeCategorySlug,
    searchQuery,
    visibleState,
    visibleSort,
    mediaState,
    savingsBand,
  );
  const { categoriesResult, productsResult } = browseContext;

  return (
    <CatalogPage
      culture={culture}
      categories={categoriesResult.data?.items ?? []}
      products={visibleWindow.items}
      activeCategorySlug={activeCategorySlug}
      totalProducts={visibleWindow.total}
      currentPage={visibleWindow.currentPage}
      pageSize={pageSize}
      searchQuery={searchQuery}
      visibleState={visibleState}
      visibleSort={visibleSort}
      mediaState={mediaState}
      savingsBand={savingsBand}
      dataStatus={{
        categories: categoriesResult.status,
        products: matchingSetResult?.status ?? productsResult.status,
      }}
    />
  );
}
