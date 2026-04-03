import "server-only";
import {
  getCatalogDetailRouteContext,
  getCatalogIndexRouteContext,
} from "@/features/catalog/server/get-catalog-route-context";
import { createPublicDiscoveryPageLoader } from "@/features/storefront/server/create-public-discovery-page-loader";
import { summarizeCatalogRouteHealth } from "@/lib/route-health";

const getCachedCatalogIndexPageContext = createPublicDiscoveryPageLoader({
  area: "catalog-page-context",
  operation: "load-index-page-context",
  thresholdMs: 300,
  getContext: (culture: string, page: number, categorySlug?: string) => ({
    culture,
    route: "/catalog",
    page,
    categorySlug: categorySlug ?? null,
  }),
  getSuccessContext: summarizeCatalogRouteHealth,
  loadRouteContext: async (culture: string, page: number, categorySlug?: string) =>
    getCatalogIndexRouteContext(culture, page, categorySlug),
});

const getCachedCatalogDetailPageContext = createPublicDiscoveryPageLoader({
  area: "catalog-page-context",
  operation: "load-detail-page-context",
  thresholdMs: 300,
  getContext: (culture: string, slug: string) => ({
    culture,
    route: "/catalog/[slug]",
    slug,
  }),
  getSuccessContext: summarizeCatalogRouteHealth,
  loadRouteContext: async (culture: string, slug: string) =>
    getCatalogDetailRouteContext(culture, slug),
});

export function getCatalogIndexPageContext(
  culture: string,
  page: number,
  categorySlug?: string,
) {
  return getCachedCatalogIndexPageContext(culture, page, categorySlug);
}

export function getCatalogDetailPageContext(culture: string, slug: string) {
  return getCachedCatalogDetailPageContext(culture, slug);
}
