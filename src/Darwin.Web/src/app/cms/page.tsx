import { CmsPagesIndex } from "@/components/cms/cms-pages-index";
import {
  readCmsMetadataFocus,
  readCmsVisibleSort,
  readCmsVisibleState,
} from "@/features/cms/discovery";
import { getCmsIndexPageContext } from "@/features/cms/server/get-cms-page-context";
import { getCmsIndexSeoMetadata } from "@/features/cms/server/get-cms-index-seo-metadata";
import {
  readPositiveIntegerSearchParam,
  readSearchTextParam,
} from "@/features/checkout/helpers";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata({
  searchParams,
}: {
  searchParams?: Promise<{
    page?: string;
    search?: string;
      visibleQuery?: string;
      visibleState?: string;
      visibleSort?: string;
      metadataFocus?: string;
  }>;
}) {
  const culture = await getRequestCulture();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const safePage = readPositiveIntegerSearchParam(resolvedSearchParams?.page);
  const search = readSearchTextParam(resolvedSearchParams?.search);
  const visibleQuery = readSearchTextParam(resolvedSearchParams?.visibleQuery);
  const visibleState = readCmsVisibleState(resolvedSearchParams?.visibleState);
  const visibleSort = readCmsVisibleSort(resolvedSearchParams?.visibleSort);
  const metadataFocus = readCmsMetadataFocus(
    resolvedSearchParams?.metadataFocus,
  );
  const { metadata } = await getCmsIndexSeoMetadata(
    culture,
    safePage,
    search,
    visibleQuery,
    visibleState,
    visibleSort,
    metadataFocus,
  );
  return metadata;
}

type CmsIndexRouteProps = {
  searchParams?: Promise<{
    page?: string;
    search?: string;
    visibleQuery?: string;
    visibleState?: string;
    visibleSort?: string;
    metadataFocus?: string;
  }>;
};

export default async function CmsIndexRoute({
  searchParams,
}: CmsIndexRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const culture = await getRequestCulture();
  const safePage = readPositiveIntegerSearchParam(resolvedSearchParams?.page);
  const searchQuery = readSearchTextParam(
    resolvedSearchParams?.search ?? resolvedSearchParams?.visibleQuery,
  );
  const visibleState = readCmsVisibleState(resolvedSearchParams?.visibleState);
  const visibleSort = readCmsVisibleSort(resolvedSearchParams?.visibleSort);
  const metadataFocus = readCmsMetadataFocus(
    resolvedSearchParams?.metadataFocus,
  );
  const {
    browseContext,
    continuationSlice,
    visibleWindow,
    metadataSummary,
    matchingItemsTotal,
    pageSize,
    matchingSetResult,
  } = await getCmsIndexPageContext(
    culture,
    safePage,
    searchQuery,
    visibleState,
    visibleSort,
    metadataFocus,
  );
  const { pagesResult } = browseContext;

  return (
    <CmsPagesIndex
      culture={culture}
      pages={visibleWindow.items}
      loadedPageCount={visibleWindow.items.length}
      totalItems={visibleWindow.total}
      matchingItemsTotal={matchingItemsTotal}
      pageSize={pageSize}
      totalPages={visibleWindow.totalPages}
      currentPage={visibleWindow.currentPage}
      status={matchingSetResult?.status ?? pagesResult.status}
      searchQuery={searchQuery}
      visibleState={visibleState}
      visibleSort={visibleSort}
      metadataFocus={metadataFocus}
      metadataSummary={metadataSummary}
      categories={continuationSlice.categories}
      categoriesStatus={continuationSlice.categoriesStatus}
      products={continuationSlice.products}
      productsStatus={continuationSlice.productsStatus}
      cartSummary={continuationSlice.cartSummary}
    />
  );
}
