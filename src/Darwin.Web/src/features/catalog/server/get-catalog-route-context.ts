import "server-only";
import { getCatalogBrowseContext } from "@/features/catalog/server/get-catalog-browse-context";
import { getProductDetailContext } from "@/features/catalog/server/get-product-detail-context";
import { getPublicStorefrontContext } from "@/features/storefront/server/get-public-storefront-context";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { summarizeCatalogRouteHealth } from "@/lib/route-health";
import {
  catalogIndexRouteObservationContext,
  productDetailRouteObservationContext,
} from "@/lib/route-observation-context";

const getCachedCatalogIndexRouteContext = createCachedObservedLoader({
  area: "catalog",
  operation: "load-route-context",
  thresholdMs: 325,
  getContext: (culture: string, page: number, categorySlug?: string) =>
    catalogIndexRouteObservationContext(culture, page, categorySlug),
  getSuccessContext: summarizeCatalogRouteHealth,
  load: async (culture: string, page: number, categorySlug?: string) => {
    const [browseContext, storefrontContext] = await Promise.all([
      getCatalogBrowseContext(culture, page, categorySlug),
      getPublicStorefrontContext(culture),
    ]);

    return {
      browseContext,
      storefrontContext,
    };
  },
});

const getCachedCatalogDetailRouteContext = createCachedObservedLoader({
  area: "product-detail",
  operation: "load-route-context",
  thresholdMs: 325,
  getContext: (culture: string, slug: string) =>
    productDetailRouteObservationContext(culture, slug),
  getSuccessContext: summarizeCatalogRouteHealth,
  load: async (culture: string, slug: string) => {
    const [detailContext, storefrontContext] = await Promise.all([
      getProductDetailContext(culture, slug),
      getPublicStorefrontContext(culture),
    ]);

    return {
      detailContext,
      storefrontContext,
    };
  },
});

export async function getCatalogIndexRouteContext(
  culture: string,
  page: number,
  categorySlug?: string,
) {
  return getCachedCatalogIndexRouteContext(culture, page, categorySlug);
}

export async function getCatalogDetailRouteContext(culture: string, slug: string) {
  return getCachedCatalogDetailRouteContext(culture, slug);
}
