import "server-only";
import { fetchPublicJson } from "@/lib/api/fetch-public-json";
import { buildQuerySuffix } from "@/lib/query-params";
import type {
  PagedResponse,
  PublicCategorySummary,
  PublicProductDetail,
  PublicProductSummary,
} from "@/features/catalog/types";

export async function getPublicCategories(culture?: string) {
  return fetchPublicJson<PagedResponse<PublicCategorySummary>>(
    `/api/v1/public/catalog/categories${buildQuerySuffix({
      page: "1",
      pageSize: "100",
      culture,
    })}`,
    "catalog-categories",
  );
}

export async function getPublicProducts(input: {
  page?: number;
  pageSize?: number;
  culture?: string;
  categorySlug?: string;
  search?: string;
}) {
  return fetchPublicJson<PagedResponse<PublicProductSummary>>(
    `/api/v1/public/catalog/products${buildQuerySuffix({
      page: String(input.page ?? 1),
      pageSize: String(input.pageSize ?? 12),
      culture: input.culture,
      categorySlug: input.categorySlug,
      search: input.search,
    })}`,
    "catalog-products",
  );
}

export async function getPublicProductSet(input?: {
  culture?: string;
  categorySlug?: string;
  search?: string;
}) {
  const initialResult = await getPublicProducts({
    page: 1,
    pageSize: 100,
    culture: input?.culture,
    categorySlug: input?.categorySlug,
    search: input?.search,
  });

  const total = initialResult.data?.total ?? 0;
  const loadedCount = initialResult.data?.items.length ?? 0;

  if (
    initialResult.status !== "ok" ||
    !initialResult.data ||
    total <= loadedCount
  ) {
    return initialResult;
  }

  const expandedResult = await getPublicProducts({
    page: 1,
    pageSize: total,
    culture: input?.culture,
    categorySlug: input?.categorySlug,
    search: input?.search,
  });

  return expandedResult.status === "ok" && expandedResult.data
    ? expandedResult
    : initialResult;
}

export async function getPublicProductBySlug(slug: string, culture?: string) {
  return fetchPublicJson<PublicProductDetail>(
    `/api/v1/public/catalog/products/${encodeURIComponent(slug)}${buildQuerySuffix({
      culture,
    })}`,
    "catalog-product-detail",
  );
}
