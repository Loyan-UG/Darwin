import "server-only";
import { getLocalizedPageInventory } from "@/features/cms/server/get-localized-page-inventory";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeLocalizedAlternatesMapHealth } from "@/lib/route-health";
import { cmsLocalizedInventoryObservationContext } from "@/lib/route-observation-context";
import { getSupportedCultures } from "@/lib/request-culture";
import { mapLocalizedDetailAlternatesById } from "@/lib/sitemap-helpers";

export const getCmsLanguageAlternatesMap = createCachedObservedLoader({
  area: "cms",
  operation: "load-language-alternates-map",
  thresholdMs: 200,
  getContext: () => cmsLocalizedInventoryObservationContext(getSupportedCultures()),
  getSuccessContext: summarizeLocalizedAlternatesMapHealth,
  load: async () => {
    const localizedInventory = await getLocalizedPageInventory();

    return mapLocalizedDetailAlternatesById(localizedInventory, (slug) =>
      `/cms/${encodeURIComponent(slug)}`,
    );
  },
});
