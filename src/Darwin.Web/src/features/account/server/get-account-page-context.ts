import "server-only";
import { getPublicAccountRouteContext } from "@/features/account/server/get-public-auth-route-context";
import { getMemberSession } from "@/features/member-session/cookies";
import { getMemberDashboardRouteContext } from "@/features/member-portal/server/get-member-route-context";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizePublicStorefrontHealth } from "@/lib/route-health";
import { summarizeAccountPageHealth } from "@/lib/route-health";
import { memberRouteObservationContext } from "@/lib/route-observation-context";

type AccountPageStorefrontFootprintSource = {
  session: unknown | null;
  publicRouteContext: { storefrontContext: Parameters<typeof summarizePublicStorefrontHealth>[0] } | null;
  memberRouteContext: { storefrontContext: Parameters<typeof summarizePublicStorefrontHealth>[0] } | null;
};

export function summarizeAccountPageStorefrontFootprint(
  result: AccountPageStorefrontFootprintSource,
) {
  const storefrontContext =
    result.memberRouteContext?.storefrontContext ??
    result.publicRouteContext?.storefrontContext ??
    null;

  if (!storefrontContext) {
    return `session:${result.session ? "present" : "missing"}|storefront:missing`;
  }

  return `session:${result.session ? "present" : "missing"}|cms:${storefrontContext.cmsPagesStatus}:${storefrontContext.cmsPages.length}|categories:${storefrontContext.categoriesStatus}:${storefrontContext.categories.length}|products:${storefrontContext.productsStatus}:${storefrontContext.products.length}|cart:${storefrontContext.storefrontCartStatus}`;
}

export const getAccountPageContext = createCachedObservedLoader({
  area: "account-page-context",
  operation: "load-page-context",
  thresholdMs: 300,
  getContext: (culture: string) => memberRouteObservationContext(culture, "/account"),
  getSuccessContext: (result) => ({
    ...summarizeAccountPageHealth(result),
    accountPageStorefrontFootprint:
      summarizeAccountPageStorefrontFootprint(result),
  }),
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
