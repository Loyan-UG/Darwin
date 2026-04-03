import "server-only";
import { getPublicAccountRouteContext } from "@/features/account/server/get-public-auth-route-context";
import { getMemberSession } from "@/features/member-session/cookies";
import { getMemberDashboardRouteContext } from "@/features/member-portal/server/get-member-route-context";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeAccountPageHealth } from "@/lib/route-health";
import { memberRouteObservationContext } from "@/lib/route-observation-context";

export const getAccountPageContext = createCachedObservedLoader({
  area: "account-page-context",
  operation: "load-page-context",
  thresholdMs: 300,
  getContext: (culture: string) => memberRouteObservationContext(culture, "/account"),
  getSuccessContext: summarizeAccountPageHealth,
  load: async (culture: string) => {
    const session = await getMemberSession();

    if (!session) {
      return {
        session: null,
        publicRouteContext: await getPublicAccountRouteContext(culture),
        memberRouteContext: null,
      };
    }

    return {
      session,
      publicRouteContext: null,
      memberRouteContext: await getMemberDashboardRouteContext(culture),
    };
  },
});
