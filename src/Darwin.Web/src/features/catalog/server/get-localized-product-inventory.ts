import "server-only";
import { getPublicProductSet } from "@/features/catalog/api/public-catalog";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { getSupportedCultures } from "@/lib/request-culture";
import { summarizeLocalizedInventoryHealth } from "@/lib/route-health";
import { catalogLocalizedInventoryObservationContext } from "@/lib/route-observation-context";

export const getLocalizedProductInventory = createCachedObservedLoader({
  area: "catalog",
  operation: "load-localized-product-inventory",
  thresholdMs: 200,
  getContext: () => catalogLocalizedInventoryObservationContext(getSupportedCultures()),
  getSuccessContext: summarizeLocalizedInventoryHealth,
  load: async () => {
    const supportedCultures = getSupportedCultures();

    return Promise.all(
      supportedCultures.map(async (culture) => {
        const result = await getPublicProductSet(culture);
        return result.status === "ok" && result.data
          ? { culture, items: result.data.items }
          : { culture, items: [] };
      }),
    );
  },
});
