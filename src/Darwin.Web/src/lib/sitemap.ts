import "server-only";
import type { MetadataRoute } from "next";
import { getPublicProducts } from "@/features/catalog/api/public-catalog";
import { getPublishedPages } from "@/features/cms/api/public-cms";
import { buildLocalizedPath } from "@/lib/locale-routing";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

const SITEMAP_PAGE_SIZE = 100;

function toAbsoluteUrl(path: string) {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${getSiteRuntimeConfig().siteUrl}${normalizedPath}`;
}

function toSitemapEntry(path: string): MetadataRoute.Sitemap[number] {
  return {
    url: toAbsoluteUrl(path),
  };
}

async function collectPagedPaths(
  loadPage: (page: number) => Promise<{
    items: Array<{ slug: string }>;
    total: number;
    pageSize: number;
  } | null>,
  toPath: (slug: string) => string,
) {
  const collectedPaths: string[] = [];
  let page = 1;
  let totalPages = 1;

  while (page <= totalPages) {
    const result = await loadPage(page);
    if (!result) {
      break;
    }

    for (const item of result.items) {
      if (item.slug) {
        collectedPaths.push(toPath(item.slug));
      }
    }

    totalPages = Math.max(1, Math.ceil(result.total / result.pageSize));
    page += 1;
  }

  return collectedPaths;
}

export async function buildPublicSitemapEntries(): Promise<MetadataRoute.Sitemap> {
  const { defaultCulture, supportedCultures } = getSiteRuntimeConfig();
  const indexLevelLocalizedPaths = ["/", "/catalog", "/cms"].flatMap((path) =>
    supportedCultures.map((culture) => buildLocalizedPath(path, culture)),
  );
  const staticEntries = Array.from(new Set(indexLevelLocalizedPaths)).map(
    toSitemapEntry,
  );

  const [cmsPaths, productPaths] = await Promise.all([
    collectPagedPaths(async (page) => {
      const result = await getPublishedPages({
        page,
        pageSize: SITEMAP_PAGE_SIZE,
      });
      const data = result.data;

      if (result.status !== "ok" || !data) {
        return null;
      }

      return {
        items: data.items,
        total: data.total,
        pageSize: data.request.pageSize || SITEMAP_PAGE_SIZE,
      };
    }, (slug) => `/cms/${encodeURIComponent(slug)}`),
    collectPagedPaths(async (page) => {
      const result = await getPublicProducts({
        page,
        pageSize: SITEMAP_PAGE_SIZE,
        culture: defaultCulture,
      });
      const data = result.data;

      if (result.status !== "ok" || !data) {
        return null;
      }

      return {
        items: data.items,
        total: data.total,
        pageSize: data.request.pageSize || SITEMAP_PAGE_SIZE,
      };
    }, (slug) => `/catalog/${encodeURIComponent(slug)}`),
  ]);

  const dedupedPaths = Array.from(new Set([...cmsPaths, ...productPaths]));
  return [...staticEntries, ...dedupedPaths.map(toSitemapEntry)];
}
