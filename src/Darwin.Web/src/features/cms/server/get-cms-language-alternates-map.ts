import "server-only";
import { getLocalizedPublicDiscoveryInventory } from "@/features/storefront/server/get-localized-public-discovery-inventory";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeLocalizedAlternatesMapHealth } from "@/lib/route-health";
import { cmsLocalizedInventoryObservationContext } from "@/lib/route-observation-context";
import { getSupportedCultures } from "@/lib/request-culture";

export const getCmsLanguageAlternatesMap = createCachedObservedLoader({
  area: "cms",
  operation: "load-language-alternates-map",
  thresholdMs: 200,
  getContext: () => cmsLocalizedInventoryObservationContext(getSupportedCultures()),
  getSuccessContext: summarizeLocalizedAlternatesMapHealth,
  load: async () => {
    const inventory = await getLocalizedPublicDiscoveryInventory();
    return inventory.pageAlternatesById;
  },
});
