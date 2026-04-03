import { CatalogPage } from "@/components/catalog/catalog-page";
import { getPublicCart } from "@/features/cart/api/public-cart";
import { getAnonymousCartId } from "@/features/cart/cookies";
import {
  getPublicCategories,
  getPublicProducts,
} from "@/features/catalog/api/public-catalog";
import {
  readAllowedSearchParam,
  readPositiveIntegerSearchParam,
  readSearchTextParam,
} from "@/features/checkout/helpers";
import {
  filterCatalogVisibleProducts,
  readCatalogVisibleState,
  sortCatalogVisibleProducts,
} from "@/features/catalog/discovery";
import type {
  CatalogVisibleState,
  CatalogVisibleSort,
} from "@/features/catalog/types";
import { buildAppQueryPath } from "@/lib/locale-routing";
import { getCatalogResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { observeAsyncOperation } from "@/lib/route-observability";
import { buildSeoMetadata } from "@/lib/seo";
import { getStorefrontContinuationContext } from "@/features/storefront/server/get-storefront-continuation-context";

function buildCatalogPath(
  category?: string,
  page?: number,
  visibleQuery?: string,
  visibleState?: CatalogVisibleState,
  visibleSort?: CatalogVisibleSort,
) {
  return buildAppQueryPath("/catalog", {
    category,
    page: page && page > 1 ? page : undefined,
    visibleQuery,
    visibleState: visibleState && visibleState !== "all" ? visibleState : undefined,
    visibleSort: visibleSort && visibleSort !== "featured" ? visibleSort : undefined,
  });
}

function readVisibleSort(value?: string): CatalogVisibleSort {
  return (
    readAllowedSearchParam(value, [
      "featured",
      "name-asc",
      "price-asc",
      "price-desc",
      "savings-desc",
      "offers-first",
      "base-first",
    ] as const) ?? "featured"
  );
}

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
  const copy = getCatalogResource(culture);
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const category = readSearchTextParam(resolvedSearchParams?.category, 80);
  const visibleQuery = readSearchTextParam(resolvedSearchParams?.visibleQuery, 80);
  const visibleState = readCatalogVisibleState(
    resolvedSearchParams?.visibleState,
  );
  const visibleSort = readVisibleSort(resolvedSearchParams?.visibleSort);
  const safePage = readPositiveIntegerSearchParam(resolvedSearchParams?.page);

  return buildSeoMetadata({
    culture,
    title: copy.catalogMetaTitle,
    description: copy.catalogMetaDescription,
    path: buildCatalogPath(
      category,
      safePage,
      visibleQuery,
      visibleState,
      visibleSort,
    ),
    noIndex:
      Boolean(category) ||
      safePage > 1 ||
      Boolean(visibleQuery) ||
      visibleState !== "all" ||
      visibleSort !== "featured",
    allowLanguageAlternates:
      !category &&
      safePage === 1 &&
      !visibleQuery &&
      visibleState === "all" &&
      visibleSort === "featured",
  });
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
  const visibleSort = readVisibleSort(resolvedSearchParams?.visibleSort);

  const anonymousCartId = await getAnonymousCartId();
  const [categoriesResult, productsResult, storefrontContext, cartResult] =
    await observeAsyncOperation(
      {
        area: "catalog",
        operation: "load-route",
        thresholdMs: 325,
      },
      () =>
        Promise.all([
          getPublicCategories(culture),
          getPublicProducts({
            page: safePage,
            pageSize: 12,
            culture,
            categorySlug: activeCategorySlug,
          }),
          getStorefrontContinuationContext(culture),
          anonymousCartId
            ? getPublicCart(anonymousCartId)
            : Promise.resolve({ data: null, status: "not-found" as const }),
        ]),
    );
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
      cmsPages={storefrontContext.cmsPages}
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
      dataStatus={{
        categories: categoriesResult.status,
        products: productsResult.status,
        cmsPages: storefrontContext.cmsPagesStatus,
      }}
    />
  );
}
