import assert from "node:assert/strict";
import test from "node:test";
import {
  catalogBrowseObservationContext,
  catalogIndexRouteObservationContext,
  catalogLocalizedInventoryObservationContext,
  cmsBrowseObservationContext,
  cmsDetailObservationContext,
  cmsDetailRouteObservationContext,
  cmsIndexRouteObservationContext,
  cmsLocalizedInventoryObservationContext,
  commerceRouteObservationContext,
  homeCategorySpotlightsObservationContext,
  homeDiscoveryObservationContext,
  homeSeoObservationContext,
  localizedDiscoveryInventoryObservationContext,
  memberRouteObservationContext,
  memberSummaryObservationContext,
  productDetailObservationContext,
  productDetailRelatedObservationContext,
  productDetailRouteObservationContext,
  publicSitemapObservationContext,
  publicStorefrontObservationContext,
  shellObservationContext,
  storefrontContinuationObservationContext,
} from "@/lib/route-observation-context";

test("basic storefront, shell, and home observation contexts stay explicit", () => {
  assert.deepEqual(publicStorefrontObservationContext("de-DE"), {
    culture: "de-DE",
  });

  assert.deepEqual(storefrontContinuationObservationContext("en-US"), {
    culture: "en-US",
  });

  assert.deepEqual(shellObservationContext("main-navigation"), {
    menuName: "main-navigation",
  });

  assert.deepEqual(homeDiscoveryObservationContext("de-DE"), {
    culture: "de-DE",
  });

  assert.deepEqual(homeSeoObservationContext("en-US"), {
    culture: "en-US",
    route: "/",
  });

  assert.deepEqual(homeCategorySpotlightsObservationContext("de-DE", 3), {
    culture: "de-DE",
    categoryCount: 3,
  });
});

test("CMS observation contexts keep browse, detail, and route metadata together", () => {
  assert.deepEqual(cmsBrowseObservationContext("de-DE", 2, "story"), {
    culture: "de-DE",
    page: 2,
    search: "story",
  });

  assert.deepEqual(cmsBrowseObservationContext("en-US", 1), {
    culture: "en-US",
    page: 1,
    search: null,
  });

  assert.deepEqual(cmsIndexRouteObservationContext("en-US", 1), {
    culture: "en-US",
    page: 1,
    search: null,
    route: "/cms",
  });

  assert.deepEqual(cmsIndexRouteObservationContext("de-DE", 3, "faq"), {
    culture: "de-DE",
    page: 3,
    search: "faq",
    route: "/cms",
  });

  assert.deepEqual(cmsDetailObservationContext("de-DE", "faq"), {
    culture: "de-DE",
    slug: "faq",
  });

  assert.deepEqual(cmsDetailRouteObservationContext("de-DE", "faq"), {
    culture: "de-DE",
    slug: "faq",
    route: "/page/[slug]",
  });
});

test("catalog observation contexts keep browse filters and route metadata explicit", () => {
  assert.deepEqual(catalogBrowseObservationContext("de-DE", 2), {
    culture: "de-DE",
    page: 2,
    categorySlug: null,
    search: null,
  });

  assert.deepEqual(
    catalogBrowseObservationContext("en-US", 1, "snacks", "chips"),
    {
      culture: "en-US",
      page: 1,
      categorySlug: "snacks",
      search: "chips",
    },
  );

  assert.deepEqual(catalogIndexRouteObservationContext("en-US", 1, "snacks"), {
    culture: "en-US",
    page: 1,
    categorySlug: "snacks",
    search: null,
    route: "/catalog",
  });

  assert.deepEqual(
    catalogIndexRouteObservationContext("de-DE", 3, undefined, "coffee"),
    {
      culture: "de-DE",
      page: 3,
      categorySlug: null,
      search: "coffee",
      route: "/catalog",
    },
  );
});

test("product detail observation contexts keep slug and category follow-up context explicit", () => {
  assert.deepEqual(productDetailObservationContext("en-US", "sea-salt-chips"), {
    culture: "en-US",
    slug: "sea-salt-chips",
  });

  assert.deepEqual(
    productDetailRelatedObservationContext(
      "en-US",
      "sea-salt-chips",
      "snacks",
    ),
    {
      culture: "en-US",
      slug: "sea-salt-chips",
      categorySlug: "snacks",
    },
  );

  assert.deepEqual(
    productDetailRouteObservationContext("de-DE", "coffee-machine"),
    {
      culture: "de-DE",
      slug: "coffee-machine",
      route: "/catalog/[slug]",
    },
  );
});

test("commerce observation contexts keep all active route families serializable", () => {
  assert.deepEqual(commerceRouteObservationContext("en-US", "/cart"), {
    culture: "en-US",
    route: "/cart",
  });

  assert.deepEqual(
    commerceRouteObservationContext("de-DE", "/checkout", {
      cartState: "present",
      itemCount: 2,
    }),
    {
      culture: "de-DE",
      route: "/checkout",
      cartState: "present",
      itemCount: 2,
    },
  );

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

  assert.deepEqual(
    commerceRouteObservationContext("en-US", "/mock-checkout", {
      returnTarget: "/checkout/orders/123/confirmation/finalize",
    }),
    {
      culture: "en-US",
      route: "/mock-checkout",
      returnTarget: "/checkout/orders/123/confirmation/finalize",
    },
  );
});

test("member observation contexts keep summary scopes and route families explicit", () => {
  assert.deepEqual(memberSummaryObservationContext("identity"), {
    scope: "identity",
  });

  assert.deepEqual(
    memberSummaryObservationContext("commerce-summary", {
      orderCount: 4,
      invoiceCount: 2,
    }),
    {
      scope: "commerce-summary",
      orderCount: 4,
      invoiceCount: 2,
    },
  );

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

  assert.deepEqual(memberRouteObservationContext("en-US", "/orders/[id]"), {
    culture: "en-US",
    route: "/orders/[id]",
  });

  assert.deepEqual(
    memberRouteObservationContext("de-DE", "/invoices/[id]", {
      invoiceId: "inv-1",
    }),
    {
      culture: "de-DE",
      route: "/invoices/[id]",
      invoiceId: "inv-1",
    },
  );

  assert.deepEqual(
    memberRouteObservationContext("en-US", "/loyalty/[businessId]", {
      businessId: "biz-1",
    }),
    {
      culture: "en-US",
      route: "/loyalty/[businessId]",
      businessId: "biz-1",
    },
  );
});

test("localized observation contexts canonicalize culture lists across every active scope", () => {
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
});

test("localized observation contexts keep empty culture lists explicit across every active scope", () => {
  assert.deepEqual(publicSitemapObservationContext(["", "  "]), {
    cultures: "",
    cultureCount: 0,
    cultureFootprint: "none",
    scope: "public-sitemap",
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
});
