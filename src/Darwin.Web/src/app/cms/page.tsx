import { CmsPagesIndex } from "@/components/cms/cms-pages-index";
import {
  filterVisiblePages,
  readCmsVisibleSort,
  readCmsVisibleState,
  sortVisiblePages,
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
    visibleQuery?: string;
    visibleState?: string;
    visibleSort?: string;
  }>;
}) {
  const culture = await getRequestCulture();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const { metadata } = await getCmsIndexSeoMetadata(culture, resolvedSearchParams);
  return metadata;
}

type CmsIndexRouteProps = {
  searchParams?: Promise<{
    page?: string;
    visibleQuery?: string;
    visibleState?: string;
    visibleSort?: string;
  }>;
};

export default async function CmsIndexRoute({
  searchParams,
}: CmsIndexRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const culture = await getRequestCulture();
  const safePage = readPositiveIntegerSearchParam(resolvedSearchParams?.page);
  const visibleQuery = readSearchTextParam(resolvedSearchParams?.visibleQuery);
  const visibleState = readCmsVisibleState(resolvedSearchParams?.visibleState);
  const visibleSort = readCmsVisibleSort(resolvedSearchParams?.visibleSort);
  const { browseContext, continuationSlice } = await getCmsIndexPageContext(
    culture,
    safePage,
  );
  const { pagesResult } = browseContext;
  const visiblePages = sortVisiblePages(
    filterVisiblePages(pagesResult.data?.items ?? [], visibleState, visibleQuery),
    visibleSort,
  );

  return (
    <CmsPagesIndex
      culture={culture}
      pages={visiblePages}
      loadedPageCount={pagesResult.data?.items.length ?? 0}
      totalItems={pagesResult.data?.total ?? 0}
      pageSize={pagesResult.data?.request.pageSize ?? 12}
      totalPages={Math.max(
        1,
        Math.ceil(
          (pagesResult.data?.total ?? 0) /
            (pagesResult.data?.request.pageSize ?? 12),
        ),
      )}
      currentPage={safePage}
      status={pagesResult.status}
      visibleQuery={visibleQuery}
      visibleState={visibleState}
      visibleSort={visibleSort}
      categories={continuationSlice.categories}
      categoriesStatus={continuationSlice.categoriesStatus}
      products={continuationSlice.products}
      productsStatus={continuationSlice.productsStatus}
      cartSummary={continuationSlice.cartSummary}
    />
  );
}
