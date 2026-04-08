import "server-only";
import { cache } from "react";
import { fetchPublicJson } from "@/lib/api/fetch-public-json";
import {
  selectExpandedPublicPagedSet,
  shouldExpandPublicPagedSet,
} from "@/lib/expand-public-paged-set";
import { buildQuerySuffix } from "@/lib/query-params";
import { normalizePublishedPageSetInput } from "@/features/cms/api/public-cms-set-input";
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
  search?: string;
}) {
  return fetchPublicJson<PagedResponse<PublicPageSummary>>(
    `/api/v1/public/cms/pages${buildQuerySuffix({
      page: String(input?.page ?? 1),
      pageSize: String(input?.pageSize ?? 24),
      culture: input?.culture,
      search: input?.search,
    })}`,
    "cms-pages",
  );
}

export async function getPublishedPageSet(input?: {
  culture?: string;
  search?: string;
}) {
  const normalizedInput = normalizePublishedPageSetInput(input);
  return getCachedPublishedPageSet(
    normalizedInput.culture,
    normalizedInput.search,
  );
}

const getCachedPublishedPageSet = cache(
  async (culture?: string, search?: string) => {
    const initialResult = await getPublishedPages({
      page: 1,
      pageSize: 48,
      culture,
      search,
    });

    if (!shouldExpandPublicPagedSet(initialResult)) {
      return initialResult;
    }

    const total = initialResult.data!.total;

    const expandedResult = await getPublishedPages({
      page: 1,
      pageSize: total,
      culture,
      search,
    });

    return selectExpandedPublicPagedSet(initialResult, expandedResult);
  },
);
