import "server-only";
import { cache } from "react";
import { fetchPublicJson } from "@/lib/api/fetch-public-json";
import {
  selectExpandedPublicPagedSet,
  shouldExpandPublicPagedSet,
} from "@/lib/expand-public-paged-set";
import { buildQuerySuffix } from "@/lib/query-params";
import { normalizePublicProductSetInput } from "@/features/catalog/api/public-catalog-set-input";
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
  const normalizedInput = normalizePublicProductSetInput(input);
  return getCachedPublicProductSet(
    normalizedInput.culture,
    normalizedInput.categorySlug,
    normalizedInput.search,
  );
}

const getCachedPublicProductSet = cache(
  async (culture?: string, categorySlug?: string, search?: string) => {
    const initialResult = await getPublicProducts({
      page: 1,
      pageSize: 100,
      culture,
      categorySlug,
      search,
    });

    if (!shouldExpandPublicPagedSet(initialResult)) {
      return initialResult;
    }

    const total = initialResult.data!.total;

    const expandedResult = await getPublicProducts({
      page: 1,
      pageSize: total,
      culture,
      categorySlug,
      search,
    });

    return selectExpandedPublicPagedSet(initialResult, expandedResult);
  },
);

export async function getPublicProductBySlug(slug: string, culture?: string) {
  return fetchPublicJson<PublicProductDetail>(
    `/api/v1/public/catalog/products/${encodeURIComponent(slug)}${buildQuerySuffix({
      culture,
    })}`,
    "catalog-product-detail",
  );
}
