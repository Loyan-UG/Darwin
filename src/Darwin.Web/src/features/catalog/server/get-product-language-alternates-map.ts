import "server-only";
import { getLocalizedProductInventory } from "@/features/catalog/server/get-localized-product-inventory";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeLocalizedAlternatesMapHealth } from "@/lib/route-health";
import { catalogLocalizedInventoryObservationContext } from "@/lib/route-observation-context";
import { getSupportedCultures } from "@/lib/request-culture";
import { mapLocalizedDetailAlternatesById } from "@/lib/sitemap-helpers";

export const getProductLanguageAlternatesMap = createCachedObservedLoader({
  area: "catalog",
  operation: "load-language-alternates-map",
  thresholdMs: 200,
  getContext: () =>
    catalogLocalizedInventoryObservationContext(getSupportedCultures()),
  getSuccessContext: summarizeLocalizedAlternatesMapHealth,
  load: async () => {
    const localizedInventory = await getLocalizedProductInventory();

    return mapLocalizedDetailAlternatesById(localizedInventory, (slug) =>
      `/catalog/${encodeURIComponent(slug)}`,
    );
  },
});
