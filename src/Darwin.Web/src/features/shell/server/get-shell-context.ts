import "server-only";
import { getPublicMenuByName } from "@/features/cms/api/public-cms";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeShellHealth } from "@/lib/route-health";
import { shellObservationContext } from "@/lib/route-observation-context";

const getCachedShellContext = createCachedObservedLoader({
  area: "shell",
  operation: "load-main-navigation",
  thresholdMs: 250,
  getContext: (menuName: string) => shellObservationContext(menuName),
  getSuccessContext: summarizeShellHealth,
  load: (menuName: string) => getPublicMenuByName(menuName),
});

export async function getShellContext(menuName: string) {
  return getCachedShellContext(menuName);
}
