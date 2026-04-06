import "server-only";
import { getPublicProductSet } from "@/features/catalog/api/public-catalog";
import { getPublishedPageSet } from "@/features/cms/api/public-cms";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeLocalizedDiscoveryInventoryHealth } from "@/lib/route-health";
import { localizedDiscoveryInventoryObservationContext } from "@/lib/route-observation-context";
import { getSupportedCultures } from "@/lib/request-culture";

export const getLocalizedPublicDiscoveryInventory = createCachedObservedLoader({
  area: "public-discovery",
  operation: "load-localized-discovery-inventory",
  thresholdMs: 225,
  getContext: () =>
    localizedDiscoveryInventoryObservationContext(getSupportedCultures()),
  getSuccessContext: summarizeLocalizedDiscoveryInventoryHealth,
  load: async () => {
    const supportedCultures = getSupportedCultures();
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

    return {
      pages: localizedByCulture.map((entry) => ({
        culture: entry.culture,
        items: entry.pages,
      })),
      products: localizedByCulture.map((entry) => ({
        culture: entry.culture,
        items: entry.products,
      })),
    };
  },
});
