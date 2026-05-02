import "server-only";
import { getPublicProductSet } from "@/features/catalog/api/public-catalog";
import { getPublishedPageSet } from "@/features/cms/api/public-cms";
import { createLocalizedDiscoveryLoader } from "@/lib/localized-discovery-loader";
import { summarizeLocalizedDiscoveryInventoryHealth } from "@/lib/route-health";
import { localizedDiscoveryInventoryObservationContext } from "@/lib/route-observation-context";
import { getSupportedCultures, getSupportedCulturesAsync } from "@/lib/request-culture";
import { projectLocalizedPublicDiscoveryInventory } from "@/features/storefront/server/localized-public-discovery-projections";

export const getLocalizedPublicDiscoveryInventory = createLocalizedDiscoveryLoader({
  kind: "inventory",
  area: "public-discovery",
  operation: "load-localized-discovery-inventory",
  thresholdMs: 325,
  getContext: () =>
    localizedDiscoveryInventoryObservationContext(getSupportedCultures()),
  getSuccessContext: summarizeLocalizedDiscoveryInventoryHealth,
  load: async () => {
    const supportedCultures = await getSupportedCulturesAsync();
    const localizedByCulture = await Promise.all(
      supportedCultures.map(async (culture) => {
        const [pagesResult, productsResult] = await Promise.all([
          getPublishedPageSet({ culture }),
          getPublicProductSet({ culture }),
        ]);

        return {
          culture,
          pages:
            pagesResult.status === "ok" && pagesResult.data
              ? pagesResult.data.items
              : [],
          products:
            productsResult.status === "ok" && productsResult.data
              ? productsResult.data.items
              : [],
        };
      }),
    );

    return projectLocalizedPublicDiscoveryInventory(localizedByCulture);
  },
});
