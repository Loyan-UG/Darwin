import { CatalogPage } from "@/components/catalog/catalog-page";
import {
  getPublicCategories,
  getPublicProducts,
} from "@/features/catalog/api/public-catalog";
import type {
  CatalogVisibleSort,
  PublicProductSummary,
} from "@/features/catalog/types";
import { getCatalogResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { buildSeoMetadata } from "@/lib/seo";

function buildCatalogPath(
  category?: string,
  page?: number,
  visibleQuery?: string,
  visibleSort?: CatalogVisibleSort,
) {
  const searchParams = new URLSearchParams();
  if (category) {
    searchParams.set("category", category);
  }
  if (page && page > 1) {
    searchParams.set("page", String(page));
  }
  if (visibleQuery) {
    searchParams.set("visibleQuery", visibleQuery);
  }
  if (visibleSort && visibleSort !== "featured") {
    searchParams.set("visibleSort", visibleSort);
  }

  const query = searchParams.toString();
  return query ? `/catalog?${query}` : "/catalog";
}

function readVisibleSort(value?: string): CatalogVisibleSort {
  switch (value) {
    case "name-asc":
    case "price-asc":
    case "price-desc":
    case "savings-desc":
      return value;
    default:
      return "featured";
  }
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
  const category = resolvedSearchParams?.category?.trim() || undefined;
  const visibleQuery = resolvedSearchParams?.visibleQuery?.trim() || undefined;
  const visibleSort = readVisibleSort(resolvedSearchParams?.visibleSort);
  const page = Number(resolvedSearchParams?.page ?? "1");
  const safePage = Number.isFinite(page) && page > 0 ? page : 1;

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
  const page = Number(resolvedSearchParams?.page ?? "1");
  const safePage = Number.isFinite(page) && page > 0 ? page : 1;
  const activeCategorySlug = resolvedSearchParams?.category?.trim() || undefined;
  const visibleQuery = resolvedSearchParams?.visibleQuery?.trim() || undefined;
  const visibleSort = readVisibleSort(resolvedSearchParams?.visibleSort);

  const [categoriesResult, productsResult] = await Promise.all([
    getPublicCategories(culture),
    getPublicProducts({
      page: safePage,
      pageSize: 12,
      culture,
      categorySlug: activeCategorySlug,
    }),
  ]);
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
      dataStatus={{
        categories: categoriesResult.status,
        products: productsResult.status,
      }}
    />
  );
}
