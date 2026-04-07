import "server-only";
import { getLocalizedPublicDiscoveryInventory } from "@/features/storefront/server/get-localized-public-discovery-inventory";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeLocalizedAlternatesMapHealth } from "@/lib/route-health";
import { catalogLocalizedInventoryObservationContext } from "@/lib/route-observation-context";
import { getSupportedCultures } from "@/lib/request-culture";

export const getProductLanguageAlternatesMap = createCachedObservedLoader({
  area: "catalog",
  operation: "load-language-alternates-map",
  thresholdMs: 200,
  getContext: () =>
    catalogLocalizedInventoryObservationContext(getSupportedCultures()),
  getSuccessContext: summarizeLocalizedAlternatesMapHealth,
  load: async () => {
    const inventory = await getLocalizedPublicDiscoveryInventory();
    return inventory.productAlternatesById;
  },
});
