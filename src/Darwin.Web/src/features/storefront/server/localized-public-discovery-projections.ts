import { buildLocalizedPath } from "@/lib/locale-routing";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import {
  groupLocalizedDetailAlternates,
  mapLocalizedDetailAlternatesById,
} from "@/lib/sitemap-helpers";

type LocalizedDiscoveryItem = {
  id: string;
  slug: string;
};

type LocalizedDiscoveryByCulture = Array<{
  culture: string;
  pages: LocalizedDiscoveryItem[];
  products: LocalizedDiscoveryItem[];
}>;

export function projectLocalizedPublicDiscoveryInventory(
  localizedByCulture: LocalizedDiscoveryByCulture,
) {
  return {
    pages: localizedByCulture.map((entry) => ({
      culture: entry.culture,
      items: entry.pages,
    })),
    products: localizedByCulture.map((entry) => ({
      culture: entry.culture,
      items: entry.products,
    })),
    pageAlternatesById: mapLocalizedDetailAlternatesById(
      localizedByCulture.map((entry) => ({
        culture: entry.culture,
        items: entry.pages,
      })),
      (slug) => `/cms/${encodeURIComponent(slug)}`,
    ),
    productAlternatesById: mapLocalizedDetailAlternatesById(
      localizedByCulture.map((entry) => ({
        culture: entry.culture,
        items: entry.products,
      })),
      (slug) => `/catalog/${encodeURIComponent(slug)}`,
    ),
    cmsSitemapEntries: groupLocalizedDetailAlternates(
      localizedByCulture.map((entry) => ({
        culture: entry.culture,
        items: entry.pages,
      })),
      (slug) => `/cms/${encodeURIComponent(slug)}`,
    ),
    productSitemapEntries: groupLocalizedDetailAlternates(
      localizedByCulture.map((entry) => ({
        culture: entry.culture,
        items: entry.products,
      })),
      (slug) => `/catalog/${encodeURIComponent(slug)}`,
    ),
  };
}

function toAbsoluteUrl(path: string) {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${getSiteRuntimeConfig().siteUrl}${normalizedPath}`;
}

function toSitemapEntry(
  path: string,
  languageAlternates?: Record<string, string>,
) {
  return {
    url: toAbsoluteUrl(path),
    ...(languageAlternates
      ? {
          alternates: {
            languages: Object.fromEntries(
              Object.entries(languageAlternates).map(([culture, alternatePath]) => [
                culture,
                toAbsoluteUrl(alternatePath),
              ]),
            ),
          },
        }
      : {}),
  };
}

export function buildPublicSitemapEntries(input: {
  supportedCultures: string[];
  cmsSitemapEntries: Array<{
    path: string;
    languageAlternates: Record<string, string>;
  }>;
  productSitemapEntries: Array<{
    path: string;
    languageAlternates: Record<string, string>;
  }>;
}) {
  const indexLevelLocalizedPaths = ["/", "/catalog", "/cms"].flatMap((path) =>
    input.supportedCultures.map((culture) => buildLocalizedPath(path, culture)),
  );
  const staticEntries = Array.from(new Set(indexLevelLocalizedPaths)).map((path) =>
    toSitemapEntry(path),
  );
  const cmsEntries = input.cmsSitemapEntries.map((entry) =>
    toSitemapEntry(entry.path, entry.languageAlternates),
  );
  const productEntries = input.productSitemapEntries.map((entry) =>
    toSitemapEntry(entry.path, entry.languageAlternates),
  );

  return {
    entries: [...staticEntries, ...cmsEntries, ...productEntries],
    staticEntryCount: staticEntries.length,
    cmsEntryCount: cmsEntries.length,
    productEntryCount: productEntries.length,
  };
}
