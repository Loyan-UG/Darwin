import "server-only";
import {
  getCmsDetailRouteContext,
  getCmsIndexRouteContext,
} from "@/features/cms/server/get-cms-route-context";
import { createPublicDiscoveryPageLoader } from "@/features/storefront/server/create-public-discovery-page-loader";
import { summarizeCmsRouteHealth } from "@/lib/route-health";

const getCachedCmsIndexPageContext = createPublicDiscoveryPageLoader({
  area: "cms-page-context",
  operation: "load-index-page-context",
  thresholdMs: 300,
  getContext: (culture: string, page: number) => ({
    culture,
    route: "/cms",
    page,
  }),
  getSuccessContext: summarizeCmsRouteHealth,
  loadRouteContext: async (culture: string, page: number) =>
    getCmsIndexRouteContext(culture, page),
});

const getCachedCmsDetailPageContext = createPublicDiscoveryPageLoader({
  area: "cms-page-context",
  operation: "load-detail-page-context",
  thresholdMs: 300,
  getContext: (culture: string, slug: string) => ({
    culture,
    route: "/cms/[slug]",
    slug,
  }),
  getSuccessContext: summarizeCmsRouteHealth,
  loadRouteContext: async (culture: string, slug: string) =>
    getCmsDetailRouteContext(culture, slug),
  sliceOptions: {
    categoryCount: 3,
    productCount: 3,
  },
});

export function getCmsIndexPageContext(culture: string, page: number) {
  return getCachedCmsIndexPageContext(culture, page);
}

export function getCmsDetailPageContext(culture: string, slug: string) {
  return getCachedCmsDetailPageContext(culture, slug);
}
