import "server-only";
import { fetchPublicJson } from "@/lib/api/fetch-public-json";
import type {
  PagedResponse,
  PublicMenu,
  PublicPageDetail,
  PublicPageSummary,
} from "@/features/cms/types";

export async function getPublicMenuByName(name: string) {
  return fetchPublicJson<PublicMenu>(
    `/api/v1/public/cms/menus/${encodeURIComponent(name)}`,
    "cms-menu",
  );
}

export async function getPublicPageBySlug(slug: string) {
  return fetchPublicJson<PublicPageDetail>(
    `/api/v1/public/cms/pages/${encodeURIComponent(slug)}`,
    "cms-page",
  );
}

export async function getPublishedPages(input?: {
  page?: number;
  pageSize?: number;
}) {
  const searchParams = new URLSearchParams({
    page: String(input?.page ?? 1),
    pageSize: String(input?.pageSize ?? 24),
  });

  return fetchPublicJson<PagedResponse<PublicPageSummary>>(
    `/api/v1/public/cms/pages?${searchParams.toString()}`,
    "cms-pages",
  );
}
