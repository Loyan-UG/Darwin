import "server-only";
import { getPublicAuthStorefrontContext } from "@/features/account/server/get-public-auth-storefront-context";
import { getMemberSession } from "@/features/member-session/cookies";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { normalizePublicAuthRouteArgs } from "@/lib/route-context-normalization";
import {
  buildMemberEntryFootprint,
  buildSharedContextBaseDiagnostics,
} from "@/lib/shared-context-diagnostics";
import { summarizeProtectedMemberEntryHealth } from "@/lib/route-health";

const getCachedMemberEntryContext = createCachedObservedLoader({
  area: "member-entry-context",
  operation: "load-entry-context",
  thresholdMs: 250,
  normalizeArgs: normalizePublicAuthRouteArgs,
  getContext: (culture: string, route: string) => ({
    culture,
    route,
  }),
  getSuccessContext: (result) => {
    const summary = summarizeProtectedMemberEntryHealth(result);
    const sessionState = result.session ? "present" : "missing";
    const storefrontState = result.storefrontContext ? "present" : "missing";

    return buildSharedContextBaseDiagnostics("member-entry", {
      hasCanonicalNormalization: true,
      extras: {
        ...summary,
        sharedContextFootprint: buildMemberEntryFootprint({
          sessionState,
          storefrontState,
        }),
      },
    });
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
