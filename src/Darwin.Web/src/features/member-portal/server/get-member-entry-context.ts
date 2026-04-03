import "server-only";
import { getPublicAuthStorefrontContext } from "@/features/account/server/get-public-auth-storefront-context";
import { getMemberSession } from "@/features/member-session/cookies";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeProtectedMemberEntryHealth } from "@/lib/route-health";

const getCachedMemberEntryContext = createCachedObservedLoader({
  area: "member-entry-context",
  operation: "load-entry-context",
  thresholdMs: 250,
  getContext: (culture: string, route: string) => ({
    culture,
    route,
  }),
  getSuccessContext: summarizeProtectedMemberEntryHealth,
  load: async (culture: string) => {
    const session = await getMemberSession();

    return {
      session,
      storefrontContext: session
        ? null
        : await getPublicAuthStorefrontContext(culture),
    };
  },
});

export function getMemberEntryContext(culture: string, route: string) {
  return getCachedMemberEntryContext(culture, route);
}
