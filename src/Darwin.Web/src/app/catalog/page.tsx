import { CatalogPage } from "@/components/catalog/catalog-page";
import { getPublicCart } from "@/features/cart/api/public-cart";
import { getAnonymousCartId } from "@/features/cart/cookies";
import {
  getPublicCategories,
  getPublicProducts,
} from "@/features/catalog/api/public-catalog";
import { getPublishedPages } from "@/features/cms/api/public-cms";
import {
  readAllowedSearchParam,
  readPositiveIntegerSearchParam,
  readSearchTextParam,
} from "@/features/checkout/helpers";
import type {
  CatalogVisibleSort,
  PublicProductSummary,
} from "@/features/catalog/types";
import { buildAppQueryPath } from "@/lib/locale-routing";
import { getCatalogResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { observeAsyncOperation } from "@/lib/route-observability";
import { buildSeoMetadata } from "@/lib/seo";

function buildCatalogPath(
  category?: string,
  page?: number,
  visibleQuery?: string,
  visibleSort?: CatalogVisibleSort,
) {
  return buildAppQueryPath("/catalog", {
    category,
    page: page && page > 1 ? page : undefined,
    visibleQuery,
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
    ] as const) ?? "featured"
  );
}

function matchesVisibleQuery(product: PublicProductSummary, query: string) {
  const normalizedQuery = query.trim().toLowerCase();
  if (!normalizedQuery) {
    return true;
  }

  return [product.name, product.shortDescription]
    .filter(Boolean)
    .some((value) => value!.toLowerCase().includes(normalizedQuery));
}

function getSavingsAmount(product: PublicProductSummary) {
  if (
    typeof product.compareAtPriceMinor !== "number" ||
    product.compareAtPriceMinor <= product.priceMinor
  ) {
    return 0;
  }

  return product.compareAtPriceMinor - product.priceMinor;
}

function applyVisibleSort(
  products: PublicProductSummary[],
  visibleSort: CatalogVisibleSort,
) {
  const rankedProducts = [...products];

  switch (visibleSort) {
    case "name-asc":
      rankedProducts.sort((left, right) => left.name.localeCompare(right.name));
      break;
    case "price-asc":
      rankedProducts.sort((left, right) => left.priceMinor - right.priceMinor);
      break;
    case "price-desc":
      rankedProducts.sort((left, right) => right.priceMinor - left.priceMinor);
      break;
    case "savings-desc":
      rankedProducts.sort(
        (left, right) => getSavingsAmount(right) - getSavingsAmount(left),
      );
      break;
    case "featured":
    default:
      break;
  }

  return rankedProducts;
}

export async function generateMetadata({
  searchParams,
}: {
  searchParams?: Promise<{
    category?: string;
    page?: string;
    visibleQuery?: string;
    visibleSort?: string;
  }>;
}) {
  const culture = await getRequestCulture();
  const copy = getCatalogResource(culture);
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const category = readSearchTextParam(resolvedSearchParams?.category, 80);
  const visibleQuery = readSearchTextParam(resolvedSearchParams?.visibleQuery, 80);
  const visibleSort = readVisibleSort(resolvedSearchParams?.visibleSort);
  const safePage = readPositiveIntegerSearchParam(resolvedSearchParams?.page);

  return buildSeoMetadata({
    culture,
    title: copy.catalogMetaTitle,
    description: copy.catalogMetaDescription,
    path: buildCatalogPath(category, safePage, visibleQuery, visibleSort),
    noIndex:
      Boolean(category) ||
      safePage > 1 ||
      Boolean(visibleQuery) ||
      visibleSort !== "featured",
    allowLanguageAlternates:
      !category &&
      safePage === 1 &&
      !visibleQuery &&
      visibleSort === "featured",
  });
}

type CatalogRouteProps = {
  searchParams?: Promise<{
    category?: string;
    page?: string;
    visibleQuery?: string;
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
  const visibleSort = readVisibleSort(resolvedSearchParams?.visibleSort);

  const anonymousCartId = await getAnonymousCartId();
  const [categoriesResult, productsResult, cmsPagesResult, cartResult] =
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
          getPublishedPages({
            page: 1,
            pageSize: 3,
            culture,
          }),
          anonymousCartId
            ? getPublicCart(anonymousCartId)
            : Promise.resolve({ data: null, status: "not-found" as const }),
        ]),
    );
  const loadedProducts = productsResult.data?.items ?? [];
  const visibleProducts = applyVisibleSort(
    visibleQuery
      ? loadedProducts.filter((product) =>
          matchesVisibleQuery(product, visibleQuery),
        )
      : loadedProducts,
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
      visibleSort={visibleSort}
      loadedProductsCount={loadedProducts.length}
      cmsPages={cmsPagesResult.data?.items ?? []}
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
        cmsPages: cmsPagesResult.status,
      }}
    />
  );
}
