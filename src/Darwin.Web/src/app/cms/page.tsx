import { CmsPagesIndex } from "@/components/cms/cms-pages-index";
import { getPublicCart } from "@/features/cart/api/public-cart";
import { getAnonymousCartId } from "@/features/cart/cookies";
import { getPublishedPages } from "@/features/cms/api/public-cms";
import {
  filterVisiblePages,
  readCmsVisibleSort,
  readCmsVisibleState,
  sortVisiblePages,
} from "@/features/cms/discovery";
import {
  readPositiveIntegerSearchParam,
  readSearchTextParam,
} from "@/features/checkout/helpers";
import { buildAppQueryPath } from "@/lib/locale-routing";
import { getSharedResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { observeAsyncOperation } from "@/lib/route-observability";
import { buildSeoMetadata } from "@/lib/seo";
import { getStorefrontContinuationContext } from "@/features/storefront/server/get-storefront-continuation-context";

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
  const shared = getSharedResource(culture);
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const safePage = readPositiveIntegerSearchParam(resolvedSearchParams?.page);
  const visibleQuery = readSearchTextParam(resolvedSearchParams?.visibleQuery);
  const visibleState = readCmsVisibleState(resolvedSearchParams?.visibleState);
  const visibleSort = readCmsVisibleSort(resolvedSearchParams?.visibleSort);

  return buildSeoMetadata({
    culture,
    title: shared.cmsIndexMetaTitle,
    description: shared.cmsIndexMetaDescription,
    path: buildAppQueryPath("/cms", {
      page: safePage > 1 ? safePage : undefined,
      visibleQuery,
      visibleState: visibleState !== "all" ? visibleState : undefined,
      visibleSort: visibleSort !== "featured" ? visibleSort : undefined,
    }),
    noIndex:
      safePage > 1 ||
      Boolean(visibleQuery) ||
      visibleState !== "all" ||
      visibleSort !== "featured",
    allowLanguageAlternates:
      safePage === 1 &&
      !visibleQuery &&
      visibleState === "all" &&
      visibleSort === "featured",
  });
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
  const anonymousCartId = await getAnonymousCartId();
  const [pagesResult, storefrontContext, cartResult] =
    await observeAsyncOperation(
      {
        area: "cms-index",
        operation: "load-route",
        thresholdMs: 325,
      },
      () =>
        Promise.all([
          getPublishedPages({
            page: safePage,
            pageSize: 12,
            culture,
          }),
          getStorefrontContinuationContext(culture),
          anonymousCartId
            ? getPublicCart(anonymousCartId)
            : Promise.resolve({ data: null, status: "not-found" as const }),
        ]),
    );
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
      categories={storefrontContext.categories}
      categoriesStatus={storefrontContext.categoriesStatus}
      products={storefrontContext.products}
      productsStatus={storefrontContext.productsStatus}
      cartSummary={
        cartResult.data
          ? {
              status: cartResult.status,
              itemCount: cartResult.data.items.length,
              currency: cartResult.data.currency,
              grandTotalGrossMinor: cartResult.data.grandTotalGrossMinor,
            }
          : null
      }
    />
  );
}
