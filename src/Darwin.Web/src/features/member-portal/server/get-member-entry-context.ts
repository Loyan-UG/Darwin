import "server-only";
import { getPublicAuthStorefrontContext } from "@/features/account/server/get-public-auth-storefront-context";
import { getMemberSession } from "@/features/member-session/cookies";
import { normalizePublicAuthRouteArgs } from "@/lib/route-context-normalization";
import {
  buildMemberEntryFootprint,
} from "@/lib/shared-context-diagnostics";
import { createSharedContextLoader } from "@/lib/shared-context-loader";
import { summarizeProtectedMemberEntryHealth } from "@/lib/route-health";
import { summarizePublicAuthStorefrontFootprint } from "@/features/account/server/get-public-auth-route-context";

const getCachedMemberEntryContext = createSharedContextLoader({
  kind: "member-entry",
  area: "member-entry-context",
  operation: "load-entry-context",
  normalizeArgs: normalizePublicAuthRouteArgs,
  getContext: (culture: string, route: string) => ({
    culture,
    route,
  }),
  getSuccessContext: (result) => {
    const summary = summarizeProtectedMemberEntryHealth(result);
    const sessionState = result.session ? "present" : "missing";
    const storefrontState = result.storefrontContext ? "present" : "missing";
    return {
      ...summary,
      sharedContextFootprint: buildMemberEntryFootprint({
        sessionState,
        storefrontState,
      }),
    memberEntryStorefrontFootprint: result.storefrontContext
      ? summarizePublicAuthStorefrontFootprint({
            storefrontContext: result.storefrontContext,
          })
        : "storefront:missing",
    };
  },
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
