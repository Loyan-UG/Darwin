import assert from "node:assert/strict";
import test from "node:test";
import {
  catalogBrowseObservationContext,
  catalogLocalizedInventoryObservationContext,
  catalogIndexRouteObservationContext,
  cmsDetailRouteObservationContext,
  cmsLocalizedInventoryObservationContext,
  commerceRouteObservationContext,
  homeCategorySpotlightsObservationContext,
  localizedDiscoveryInventoryObservationContext,
  memberRouteObservationContext,
  memberSummaryObservationContext,
  publicSitemapObservationContext,
  productDetailRelatedObservationContext,
  shellObservationContext,
} from "@/lib/route-observation-context";

test("catalog browse observation context normalizes missing category to null", () => {
  assert.deepEqual(catalogBrowseObservationContext("de-DE", 2), {
    culture: "de-DE",
    page: 2,
    categorySlug: null,
    search: null,
  });
});

test("catalog route observation context carries route and category context together", () => {
  assert.deepEqual(
    catalogIndexRouteObservationContext("en-US", 1, "snacks"),
    {
      culture: "en-US",
      page: 1,
      categorySlug: "snacks",
      search: null,
      route: "/catalog",
    },
  );
});

test("cms detail route observation context keeps slug and canonical route key", () => {
  assert.deepEqual(cmsDetailRouteObservationContext("de-DE", "faq"), {
    culture: "de-DE",
    slug: "faq",
    route: "/cms/[slug]",
  });
});

test("commerce route observation context merges route-specific metadata", () => {
  assert.deepEqual(
    commerceRouteObservationContext(
      "de-DE",
      "/checkout/orders/[orderId]/confirmation",
      {
        orderId: "123",
        hasOrderNumber: true,
      },
    ),
    {
      culture: "de-DE",
      route: "/checkout/orders/[orderId]/confirmation",
      orderId: "123",
      hasOrderNumber: true,
    },
  );
});

test("member summary and member route observation contexts keep explicit operational scope", () => {
  assert.deepEqual(memberSummaryObservationContext("identity"), {
    scope: "identity",
  });
  assert.deepEqual(
    memberRouteObservationContext("de-DE", "/orders", {
      page: 3,
      pageSize: 20,
    }),
    {
      culture: "de-DE",
      route: "/orders",
      page: 3,
      pageSize: 20,
    },
  );
});

test("home, shell, and product-related observation contexts stay serializable and precise", () => {
  assert.deepEqual(shellObservationContext("main-navigation"), {
    menuName: "main-navigation",
  });
  assert.deepEqual(homeCategorySpotlightsObservationContext("de-DE", 3), {
    culture: "de-DE",
    categoryCount: 3,
  });
  assert.deepEqual(
    productDetailRelatedObservationContext("en-US", "sea-salt-chips", "snacks"),
    {
      culture: "en-US",
      slug: "sea-salt-chips",
      categorySlug: "snacks",
    },
  );
});

test("localized discovery observation contexts canonicalize culture lists", () => {
  assert.deepEqual(
    localizedDiscoveryInventoryObservationContext([
      " en-US ",
      "de-DE",
      "en-US",
    ]),
    {
      cultures: "de-DE,en-US",
      cultureCount: 2,
      cultureFootprint: "de-DE|en-US",
      scope: "localized-discovery-inventory",
    },
  );

  assert.deepEqual(
    publicSitemapObservationContext(["en-US", "de-DE", "de-DE"]),
    {
      cultures: "de-DE,en-US",
      cultureCount: 2,
      cultureFootprint: "de-DE|en-US",
      scope: "public-sitemap",
    },
  );

  assert.deepEqual(
    cmsLocalizedInventoryObservationContext([" de-DE ", "", "en-US"]),
    {
      cultures: "de-DE,en-US",
      cultureCount: 2,
      cultureFootprint: "de-DE|en-US",
      scope: "localized-page-inventory",
    },
  );

  assert.deepEqual(
    catalogLocalizedInventoryObservationContext(["en-US", "de-DE"]),
    {
      cultures: "de-DE,en-US",
      cultureCount: 2,
      cultureFootprint: "de-DE|en-US",
      scope: "localized-product-inventory",
    },
  );

  assert.deepEqual(publicSitemapObservationContext(["", "  "]), {
    cultures: "",
    cultureCount: 0,
    cultureFootprint: "none",
    scope: "public-sitemap",
  });
});
