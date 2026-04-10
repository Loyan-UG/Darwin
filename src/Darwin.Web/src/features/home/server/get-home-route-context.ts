import "server-only";
import { getHomePageParts } from "@/features/home/get-home-page-parts";
import { getHomeDiscoveryContext } from "@/features/home/server/get-home-discovery-context";
import { getMemberSession } from "@/features/member-session/cookies";
import { createObservedLoader } from "@/lib/observed-loader";
import { summarizeHomeRouteHealth } from "@/lib/route-health";

const loadHomeRouteContext = createObservedLoader({
  area: "home-route-context",
  operation: "load-route-context",
  thresholdMs: 350,
  getContext: (culture: string) => ({ culture }),
  getSuccessContext: summarizeHomeRouteHealth,
  load: async (culture: string) => {
    const [memberSession, homeDiscoveryContext] = await Promise.all([
      getMemberSession(),
      getHomeDiscoveryContext(culture),
    ]);
    const parts = await getHomePageParts(
      culture,
      memberSession,
      homeDiscoveryContext,
    );

    return {
      memberSession,
      homeDiscoveryContext,
      parts,
    };
  },
});

export async function getHomeRouteContext(culture: string) {
  return loadHomeRouteContext(culture);
}
