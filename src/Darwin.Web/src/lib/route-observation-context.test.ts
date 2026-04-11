import assert from "node:assert/strict";
import test from "node:test";
import {
  catalogBrowseObservationContext,
  catalogLocalizedInventoryObservationContext,
  cmsBrowseObservationContext,
  cmsDetailObservationContext,
  cmsIndexRouteObservationContext,
  homeDiscoveryObservationContext,
  homeSeoObservationContext,
  productDetailObservationContext,
  productDetailRouteObservationContext,
  publicStorefrontObservationContext,
  storefrontContinuationObservationContext,
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

test("basic storefront, home, CMS, and product observation contexts stay explicit", () => {
  assert.deepEqual(publicStorefrontObservationContext("de-DE"), {
    culture: "de-DE",
  });
  assert.deepEqual(storefrontContinuationObservationContext("en-US"), {
    culture: "en-US",
  });
  assert.deepEqual(homeDiscoveryObservationContext("de-DE"), {
    culture: "de-DE",
  });
  assert.deepEqual(homeSeoObservationContext("en-US"), {
    culture: "en-US",
    route: "/",
  });
  assert.deepEqual(cmsBrowseObservationContext("de-DE", 2, "story"), {
    culture: "de-DE",
    page: 2,
    search: "story",
  });
  assert.deepEqual(cmsIndexRouteObservationContext("en-US", 1), {
    culture: "en-US",
    page: 1,
    search: null,
    route: "/cms",
  });
  assert.deepEqual(cmsDetailObservationContext("de-DE", "faq"), {
    culture: "de-DE",
    slug: "faq",
  });
  assert.deepEqual(productDetailObservationContext("en-US", "sea-salt-chips"), {
    culture: "en-US",
    slug: "sea-salt-chips",
  });
  assert.deepEqual(productDetailRouteObservationContext("de-DE", "coffee-machine"), {
    culture: "de-DE",
    slug: "coffee-machine",
    route: "/catalog/[slug]",
  });
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


test("commerce and member observation contexts keep bare route branches explicit", () => {
  assert.deepEqual(commerceRouteObservationContext("en-US", "/cart"), {
    culture: "en-US",
    route: "/cart",
  });
  assert.deepEqual(memberSummaryObservationContext("commerce-summary", {
    orderCount: 4,
    invoiceCount: 2,
  }), {
    scope: "commerce-summary",
    orderCount: 4,
    invoiceCount: 2,
  });
  assert.deepEqual(memberRouteObservationContext("en-US", "/orders/[id]"), {
    culture: "en-US",
    route: "/orders/[id]",
  });
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
  assert.deepEqual(localizedDiscoveryInventoryObservationContext(["", "  "]), {
    cultures: "",
    cultureCount: 0,
    cultureFootprint: "none",
    scope: "localized-discovery-inventory",
  });

  assert.deepEqual(cmsLocalizedInventoryObservationContext(["", "  "]), {
    cultures: "",
    cultureCount: 0,
    cultureFootprint: "none",
    scope: "localized-page-inventory",
  });

  assert.deepEqual(catalogLocalizedInventoryObservationContext(["", "  "]), {
    cultures: "",
    cultureCount: 0,
    cultureFootprint: "none",
    scope: "localized-product-inventory",
  });


