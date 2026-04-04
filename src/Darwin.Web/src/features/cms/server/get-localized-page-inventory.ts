import "server-only";
import { getPublishedPageSet } from "@/features/cms/api/public-cms";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { getSupportedCultures } from "@/lib/request-culture";
import { summarizeLocalizedInventoryHealth } from "@/lib/route-health";
import { cmsLocalizedInventoryObservationContext } from "@/lib/route-observation-context";

export const getLocalizedPageInventory = createCachedObservedLoader({
  area: "cms",
  operation: "load-localized-page-inventory",
  thresholdMs: 200,
  getContext: () => cmsLocalizedInventoryObservationContext(getSupportedCultures()),
  getSuccessContext: summarizeLocalizedInventoryHealth,
  load: async () => {
    const supportedCultures = getSupportedCultures();

    return Promise.all(
      supportedCultures.map(async (culture) => {
        const result = await getPublishedPageSet({ culture });
        return result.status === "ok" && result.data
          ? { culture, items: result.data.items }
          : { culture, items: [] };
      }),
    );
  },
});
