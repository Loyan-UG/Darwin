import "server-only";
import { getPublicMenuByName } from "@/features/cms/api/public-cms";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeShellHealth } from "@/lib/route-health";
import { shellObservationContext } from "@/lib/route-observation-context";

const getCachedShellContext = createCachedObservedLoader({
  area: "shell",
  operation: "load-main-navigation",
  thresholdMs: 250,
  getContext: (culture: string, menuName: string) => ({
    ...shellObservationContext(menuName),
    culture,
  }),
  getSuccessContext: summarizeShellHealth,
  load: (culture: string, menuName: string) => getPublicMenuByName(menuName, culture),
});

export async function getShellContext(culture: string, menuName: string) {
  return getCachedShellContext(culture, menuName);
}
