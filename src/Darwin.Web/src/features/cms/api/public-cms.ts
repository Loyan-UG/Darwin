import "server-only";
import { fetchPublicJson } from "@/lib/api/fetch-public-json";
import type {
  PagedResponse,
  PublicMenu,
  PublicPageDetail,
  PublicPageSummary,
} from "@/features/cms/types";

function buildCmsQuery(query: Record<string, string | undefined>) {
  const searchParams = new URLSearchParams();

  for (const [key, value] of Object.entries(query)) {
    if (value) {
      searchParams.set(key, value);
    }
  }

  const serialized = searchParams.toString();
  return serialized ? `?${serialized}` : "";
}

export async function getPublicMenuByName(name: string) {
  return fetchPublicJson<PublicMenu>(
    `/api/v1/public/cms/menus/${encodeURIComponent(name)}`,
    "cms-menu",
  );
}

export async function getPublicPageBySlug(slug: string, culture?: string) {
  return fetchPublicJson<PublicPageDetail>(
    `/api/v1/public/cms/pages/${encodeURIComponent(slug)}${buildCmsQuery({
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
    `/api/v1/public/cms/pages${buildCmsQuery({
      page: String(input?.page ?? 1),
      pageSize: String(input?.pageSize ?? 24),
      culture: input?.culture,
    })}`,
    "cms-pages",
  );
}
