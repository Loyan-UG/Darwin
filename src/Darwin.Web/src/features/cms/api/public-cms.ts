import "server-only";
import { fetchPublicJson } from "@/lib/api/fetch-public-json";
import { buildQuerySuffix } from "@/lib/query-params";
import type {
  PagedResponse,
  PublicMenu,
  PublicPageDetail,
  PublicPageSummary,
} from "@/features/cms/types";

export async function getPublicMenuByName(name: string, culture?: string) {
  return fetchPublicJson<PublicMenu>(
    `/api/v1/public/cms/menus/${encodeURIComponent(name)}${buildQuerySuffix({
      culture,
    })}`,
    "cms-menu",
  );
}

export async function getPublicPageBySlug(slug: string, culture?: string) {
  return fetchPublicJson<PublicPageDetail>(
    `/api/v1/public/cms/pages/${encodeURIComponent(slug)}${buildQuerySuffix({
      culture,
    })}`,
    "cms-page",
  );
}

export async function getPublishedPages(input?: {
  page?: number;
  pageSize?: number;
  culture?: string;
}) {
  return fetchPublicJson<PagedResponse<PublicPageSummary>>(
    `/api/v1/public/cms/pages${buildQuerySuffix({
      page: String(input?.page ?? 1),
      pageSize: String(input?.pageSize ?? 24),
      culture: input?.culture,
    })}`,
    "cms-pages",
  );
}

export async function getPublishedPageSet(culture?: string) {
  const initialResult = await getPublishedPages({
    page: 1,
    pageSize: 48,
    culture,
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

  const expandedResult = await getPublishedPages({
    page: 1,
    pageSize: total,
    culture,
  });

  return expandedResult.status === "ok" && expandedResult.data
    ? expandedResult
    : initialResult;
}
