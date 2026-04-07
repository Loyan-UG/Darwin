import "server-only";
import type { MetadataRoute } from "next";
import { getLocalizedPublicDiscoveryInventory } from "@/features/storefront/server/get-localized-public-discovery-inventory";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { buildLocalizedPath } from "@/lib/locale-routing";
import { summarizePublicSitemapHealth } from "@/lib/route-health";
import { publicSitemapObservationContext } from "@/lib/route-observation-context";
import { getSupportedCultures } from "@/lib/request-culture";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

function toAbsoluteUrl(path: string) {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${getSiteRuntimeConfig().siteUrl}${normalizedPath}`;
}

function toSitemapEntry(
  path: string,
  languageAlternates?: Record<string, string>,
): MetadataRoute.Sitemap[number] {
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

export const getPublicSitemapContext = createCachedObservedLoader({
  area: "public-sitemap",
  operation: "load-sitemap-context",
  thresholdMs: 250,
  getContext: () => publicSitemapObservationContext(getSupportedCultures()),
  getSuccessContext: summarizePublicSitemapHealth,
  load: async () => {
    const { supportedCultures } = getSiteRuntimeConfig();
    const indexLevelLocalizedPaths = ["/", "/catalog", "/cms"].flatMap((path) =>
      supportedCultures.map((culture) => buildLocalizedPath(path, culture)),
    );
    const staticEntries = Array.from(new Set(indexLevelLocalizedPaths)).map((path) =>
      toSitemapEntry(path),
    );

    const { cmsSitemapEntries, productSitemapEntries } =
      await getLocalizedPublicDiscoveryInventory();

    const cmsEntries = cmsSitemapEntries.map((entry) =>
      toSitemapEntry(entry.path, entry.languageAlternates),
    );
    const productEntries = productSitemapEntries.map((entry) =>
      toSitemapEntry(entry.path, entry.languageAlternates),
    );

    return {
      entries: [...staticEntries, ...cmsEntries, ...productEntries],
      staticEntryCount: staticEntries.length,
      cmsEntryCount: cmsEntries.length,
      productEntryCount: productEntries.length,
    };
  },
});
