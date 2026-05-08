import { buildCatalogProductPath, buildCmsPagePath } from "@/lib/entity-paths";
import { buildLocalizedPath } from "@/lib/locale-routing";
import { canonicalizeLanguageAlternates } from "@/lib/localized-alternates";
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
      buildCmsPagePath,
    ),
    productAlternatesById: mapLocalizedDetailAlternatesById(
      localizedByCulture.map((entry) => ({
        culture: entry.culture,
        items: entry.products,
      })),
      buildCatalogProductPath,
    ),
    cmsSitemapEntries: groupLocalizedDetailAlternates(
      localizedByCulture.map((entry) => ({
        culture: entry.culture,
        items: entry.pages,
      })),
      buildCmsPagePath,
    ),
    productSitemapEntries: groupLocalizedDetailAlternates(
      localizedByCulture.map((entry) => ({
        culture: entry.culture,
        items: entry.products,
      })),
      buildCatalogProductPath,
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
  const normalizedAlternates =
    canonicalizeLanguageAlternates(languageAlternates);

  return {
    url: toAbsoluteUrl(path),
    ...(normalizedAlternates
      ? {
          alternates: {
            languages: Object.fromEntries(
              Object.entries(normalizedAlternates).map(
                ([culture, alternatePath]) => [
                  culture,
                  toAbsoluteUrl(alternatePath),
                ],
              ),
            ),
          },
        }
      : {}),
  };
}

function canonicalizeSupportedCultures(supportedCultures: string[]) {
  const runtimeConfig = getSiteRuntimeConfig();
  const allowedCultures = new Set(runtimeConfig.supportedCultures);
  const normalizedCultures = Array.from(
    new Set(
      supportedCultures
        .map((culture) => culture.trim())
        .filter((culture) => culture.length > 0 && allowedCultures.has(culture)),
    ),
  );

  return runtimeConfig.supportedCultures.filter((culture) =>
    normalizedCultures.includes(culture),
  );
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
  const supportedCultures = canonicalizeSupportedCultures(input.supportedCultures);
  const indexLevelLocalizedPaths = ["/", "/catalog", "/help"].flatMap((path) =>
    supportedCultures.map((culture) => buildLocalizedPath(path, culture)),
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
