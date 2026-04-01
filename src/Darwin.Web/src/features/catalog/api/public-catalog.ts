import "server-only";
import { fetchPublicJson } from "@/lib/api/fetch-public-json";
import type {
  PagedResponse,
  PublicCategorySummary,
  PublicProductDetail,
  PublicProductSummary,
} from "@/features/catalog/types";

function buildCatalogQuery(query: Record<string, string | undefined>) {
  const searchParams = new URLSearchParams();

  for (const [key, value] of Object.entries(query)) {
    if (value) {
      searchParams.set(key, value);
    }
  }

  const serialized = searchParams.toString();
  return serialized ? `?${serialized}` : "";
}

export async function getPublicCategories(culture?: string) {
  return fetchPublicJson<PagedResponse<PublicCategorySummary>>(
    `/api/v1/public/catalog/categories${buildCatalogQuery({
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
}) {
  return fetchPublicJson<PagedResponse<PublicProductSummary>>(
    `/api/v1/public/catalog/products${buildCatalogQuery({
      page: String(input.page ?? 1),
      pageSize: String(input.pageSize ?? 12),
      culture: input.culture,
      categorySlug: input.categorySlug,
    })}`,
    "catalog-products",
  );
}

export async function getPublicProductBySlug(slug: string, culture?: string) {
  return fetchPublicJson<PublicProductDetail>(
    `/api/v1/public/catalog/products/${encodeURIComponent(slug)}${buildCatalogQuery({
      culture,
    })}`,
    "catalog-product-detail",
  );
}
