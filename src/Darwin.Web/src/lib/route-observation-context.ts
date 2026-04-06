export function publicStorefrontObservationContext(culture: string) {
  return { culture };
}

export function storefrontContinuationObservationContext(culture: string) {
  return { culture };
}

export function shellObservationContext(menuName: string) {
  return { menuName };
}

export function homeDiscoveryObservationContext(culture: string) {
  return { culture };
}

export function homeSeoObservationContext(culture: string) {
  return {
    culture,
    route: "/",
  };
}

export function homeCategorySpotlightsObservationContext(
  culture: string,
  categoryCount: number,
) {
  return { culture, categoryCount };
}

export function cmsBrowseObservationContext(
  culture: string,
  page: number,
  search?: string,
) {
  return { culture, page, search: search ?? null };
}

export function cmsLocalizedInventoryObservationContext(cultures: string[]) {
  return {
    cultures: cultures.join(","),
    scope: "localized-page-inventory",
  };
}

export function cmsIndexRouteObservationContext(
  culture: string,
  page: number,
  search?: string,
) {
  return { culture, page, search: search ?? null, route: "/cms" };
}

export function cmsDetailObservationContext(culture: string, slug: string) {
  return { culture, slug };
}

export function cmsDetailRouteObservationContext(culture: string, slug: string) {
  return { culture, slug, route: "/cms/[slug]" };
}

export function catalogBrowseObservationContext(
  culture: string,
  page: number,
  categorySlug?: string,
  search?: string,
) {
  return {
    culture,
    page,
    categorySlug: categorySlug ?? null,
    search: search ?? null,
  };
}

export function catalogLocalizedInventoryObservationContext(
  cultures: string[],
) {
  return {
    cultures: cultures.join(","),
    scope: "localized-product-inventory",
  };
}

export function publicSitemapObservationContext(cultures: string[]) {
  return {
    cultures: cultures.join(","),
    scope: "public-sitemap",
  };
}

export function localizedDiscoveryInventoryObservationContext(
  cultures: string[],
) {
  return {
    cultures: cultures.join(","),
    scope: "localized-discovery-inventory",
  };
}

export function catalogIndexRouteObservationContext(
  culture: string,
  page: number,
  categorySlug?: string,
  search?: string,
) {
  return {
    ...catalogBrowseObservationContext(culture, page, categorySlug, search),
    route: "/catalog",
  };
}

export function productDetailObservationContext(culture: string, slug: string) {
  return { culture, slug };
}

export function productDetailRelatedObservationContext(
  culture: string,
  slug: string,
  categorySlug: string,
) {
  return { culture, slug, categorySlug };
}

export function productDetailRouteObservationContext(
  culture: string,
  slug: string,
) {
  return { culture, slug, route: "/catalog/[slug]" };
}

export function commerceRouteObservationContext(
  culture: string,
  route: "/cart" | "/checkout" | "/checkout/orders/[orderId]/confirmation",
  extra?: Record<string, unknown>,
) {
  return {
    culture,
    route,
    ...(extra ?? {}),
  };
}

export function memberSummaryObservationContext(
  scope: "identity" | "commerce-summary",
  extra?: Record<string, unknown>,
) {
  return {
    scope,
    ...(extra ?? {}),
  };
}

export function memberRouteObservationContext(
  culture: string,
  route:
    | "/account"
    | "/orders"
    | "/invoices"
    | "/orders/[id]"
    | "/invoices/[id]",
  extra?: Record<string, unknown>,
) {
  return {
    culture,
    route,
    ...(extra ?? {}),
  };
}
