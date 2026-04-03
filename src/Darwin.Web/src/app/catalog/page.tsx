import { CatalogPage } from "@/components/catalog/catalog-page";
import {
  readPositiveIntegerSearchParam,
  readSearchTextParam,
} from "@/features/checkout/helpers";
import {
  filterCatalogVisibleProducts,
  readCatalogVisibleSort,
  readCatalogVisibleState,
  sortCatalogVisibleProducts,
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
      visibleQuery?: string;
      visibleState?: string;
      visibleSort?: string;
    }>;
}) {
  const culture = await getRequestCulture();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const { metadata } = await getCatalogIndexSeoMetadata(
    culture,
    resolvedSearchParams,
  );
  return metadata;
}

type CatalogRouteProps = {
  searchParams?: Promise<{
    category?: string;
    page?: string;
    visibleQuery?: string;
    visibleState?: string;
    visibleSort?: string;
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
  const visibleQuery = readSearchTextParam(
    resolvedSearchParams?.visibleQuery,
    80,
  );
  const visibleState = readCatalogVisibleState(
    resolvedSearchParams?.visibleState,
  );
  const visibleSort = readCatalogVisibleSort(resolvedSearchParams?.visibleSort);

  const { browseContext, continuationSlice } = await getCatalogIndexPageContext(
    culture,
    safePage,
    activeCategorySlug,
  );
  const { categoriesResult, productsResult } = browseContext;
  const loadedProducts = productsResult.data?.items ?? [];
  const visibleProducts = sortCatalogVisibleProducts(
    filterCatalogVisibleProducts(loadedProducts, visibleState, visibleQuery),
    visibleSort,
  );

  return (
    <CatalogPage
      culture={culture}
      categories={categoriesResult.data?.items ?? []}
      products={visibleProducts}
      activeCategorySlug={activeCategorySlug}
      totalProducts={productsResult.data?.total ?? 0}
      currentPage={safePage}
      pageSize={productsResult.data?.request.pageSize ?? 12}
      visibleQuery={visibleQuery}
      visibleState={visibleState}
      visibleSort={visibleSort}
      loadedProductsCount={loadedProducts.length}
      cmsPages={continuationSlice.cmsPages}
      cartSummary={continuationSlice.cartSummary}
      dataStatus={{
        categories: categoriesResult.status,
        products: productsResult.status,
        cmsPages: continuationSlice.cmsPagesStatus,
      }}
    />
  );
}
