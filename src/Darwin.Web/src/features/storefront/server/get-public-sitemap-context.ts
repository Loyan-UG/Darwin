import "server-only";
import { getLocalizedPublicDiscoveryInventory } from "@/features/storefront/server/get-localized-public-discovery-inventory";
import { buildPublicSitemapEntries } from "@/features/storefront/server/localized-public-discovery-projections";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizePublicSitemapHealth } from "@/lib/route-health";
import { publicSitemapObservationContext } from "@/lib/route-observation-context";
import { getSupportedCultures } from "@/lib/request-culture";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

export const getPublicSitemapContext = createCachedObservedLoader({
  area: "public-sitemap",
  operation: "load-sitemap-context",
  thresholdMs: 350,
  getContext: () => publicSitemapObservationContext(getSupportedCultures()),
  getSuccessContext: summarizePublicSitemapHealth,
  load: async () => {
    const { supportedCultures } = getSiteRuntimeConfig();
    const { cmsSitemapEntries, productSitemapEntries } =
      await getLocalizedPublicDiscoveryInventory();

    return buildPublicSitemapEntries({
      supportedCultures,
      cmsSitemapEntries,
      productSitemapEntries,
    });
  },
});
