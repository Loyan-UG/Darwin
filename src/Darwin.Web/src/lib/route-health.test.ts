import assert from "node:assert/strict";
import test from "node:test";
import {
  summarizeCommerceRouteHealth,
  summarizeCatalogRouteHealth,
  summarizeAccountPageHealth,
  summarizeCatalogBrowseCoreHealth,
  summarizeCatalogDetailCoreHealth,
  summarizeCartPageHealth,
  summarizeCartViewModelHealth,
  summarizeCmsRouteHealth,
  summarizeCmsBrowseCoreHealth,
  summarizeCmsDetailCoreHealth,
  summarizeCheckoutPageHealth,
  summarizeConfirmationResultHealth,
  summarizeConfirmationPageHealth,
  summarizeHomeDiscoveryHealth,
  summarizeHomeRouteHealth,
  summarizeHomeCategorySpotlightsHealth,
  summarizeLocalizedAlternatesMapHealth,
  summarizeMemberCommerceSummaryHealth,
  summarizeMemberCollectionHealth,
  summarizeMemberDashboardHealth,
  summarizeMemberDetailHealth,
  summarizeMemberEditorHealth,
  summarizeMemberIdentityHealth,
  summarizeMemberPagedCollectionHealth,
  summarizeProductDetailRelatedHealth,
  summarizePublicSitemapHealth,
  summarizePublicStorefrontHealth,
  summarizePublicAuthRouteHealth,
  summarizeProtectedMemberEntryHealth,
  summarizeSeoMetadataHealth,
  summarizeShellHealth,
  summarizeShellModelHealth,
  summarizeStorefrontContinuationHealth,
  summarizeStorefrontShoppingHealth,
} from "@/lib/route-health";
import type { PublicStorefrontContext } from "@/features/storefront/public-storefront-context";

function createStorefrontContext(): PublicStorefrontContext {
  return {
    cmsPagesResult: { data: null, status: "ok" },
    cmsPages: [{ id: "p1", slug: "one", title: "One", metaTitle: "One", metaDescription: "Desc" }],
    cmsPagesStatus: "ok",
    categoriesResult: { data: null, status: "degraded" },
    categories: [{ id: "c1", slug: "cat", name: "Cat", description: null }],
    categoriesStatus: "degraded",
    productsResult: { data: null, status: "ok" },
    products: [{ id: "pr1", slug: "prod", name: "Prod", shortDescription: null, priceMinor: 100, compareAtPriceMinor: 150, currency: "EUR", primaryImageUrl: null }],
    productsStatus: "ok",
    storefrontCart: null,
    storefrontCartStatus: "not-found",
    cartSnapshots: [],
    cartLinkedProductSlugs: ["prod"],
  };
}

test("summarizePublicStorefrontHealth exposes canonical storefront statuses and counts", () => {
  assert.deepEqual(summarizePublicStorefrontHealth(createStorefrontContext()), {
    cmsStatus: "ok",
    cmsCount: 1,
    categoriesStatus: "degraded",
    categoryCount: 1,
    productsStatus: "ok",
    productCount: 1,
    cartStatus: "not-found",
    cartLinkedCount: 1,
  });

  assert.deepEqual(
    summarizeStorefrontContinuationHealth({
      cmsPagesStatus: "ok",
      cmsPages: [1, 2],
      categoriesStatus: "degraded",
      categories: [1],
      productsStatus: "ok",
      products: [1, 2, 3],
    }),
    {
      cmsStatus: "ok",
      cmsCount: 2,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
    },
  );

  assert.deepEqual(
    summarizeShellHealth({
      status: "ok",
      data: {
        items: [1, 2, 3],
      },
    }),
    {
      menuStatus: "ok",
      menuItemCount: 3,
    },
  );

  assert.deepEqual(
    summarizeShellModelHealth({
      culture: "de-DE",
      menuSource: "cms",
      menuStatus: "ok",
      primaryNavigation: [1, 2],
      utilityLinks: [1],
      footerGroups: [1, 2, 3],
    }),
    {
      culture: "de-DE",
      menuSource: "cms",
      menuStatus: "ok",
      primaryNavigationCount: 2,
      utilityLinkCount: 1,
      footerGroupCount: 3,
    },
  );
});

test("route health helpers carry core and storefront health together", () => {
  const storefrontContext = createStorefrontContext();

  assert.equal(
    summarizeCatalogRouteHealth({
      storefrontContext,
      browseContext: {
        categoriesResult: { status: "ok", data: { items: [1, 2] } },
        productsResult: { status: "degraded", data: { items: [1] } },
      },
    }).coreProductsStatus,
    "degraded",
  );

  assert.deepEqual(
    summarizeCatalogBrowseCoreHealth({
      categoriesResult: { status: "ok", data: { items: [1, 2] } },
      productsResult: { status: "degraded", data: { items: [1] } },
    }),
    {
      categoriesStatus: "ok",
      categoryCount: 2,
      productsStatus: "degraded",
      productCount: 1,
    },
  );

  assert.deepEqual(
    summarizeCatalogDetailCoreHealth({
      categoriesResult: { status: "ok", data: { items: [1, 2] } },
      productResult: { status: "ok", data: { id: "prod-1" } },
    }),
    {
      categoriesStatus: "ok",
      categoryCount: 2,
      productStatus: "ok",
      hasProduct: true,
    },
  );

  assert.equal(
    summarizeCmsRouteHealth({
      storefrontContext,
      detailContext: {
        pageResult: { status: "ok", data: { id: "page-1" } },
        relatedPagesResult: { status: "ok", data: { items: [1, 2, 3] } },
      },
    }).corePageCount,
    3,
  );

  assert.deepEqual(
    summarizeCmsBrowseCoreHealth({
      pagesResult: { status: "ok", data: { items: [1, 2, 3] } },
    }),
    {
      pagesStatus: "ok",
      pageCount: 3,
    },
  );

  assert.deepEqual(
    summarizeCmsDetailCoreHealth({
      pageResult: { status: "ok", data: { id: "page-1" } },
      relatedPagesResult: { status: "degraded", data: { items: [1, 2] } },
    }),
    {
      pageStatus: "ok",
      hasPage: true,
      relatedSeedStatus: "degraded",
      relatedSeedCount: 2,
    },
  );
});

test("member and home health helpers summarize route readiness for diagnostics", () => {
  const storefrontContext = createStorefrontContext();

  assert.equal(
    summarizeMemberDashboardHealth({
      storefrontContext,
      identityContext: {
        profileResult: { status: "ok" },
        preferencesResult: { status: "ok" },
        customerContextResult: { status: "degraded" },
        addressesResult: { status: "ok", data: [{ id: 1 }] },
      },
      commerceSummaryContext: {
        ordersResult: { status: "ok", data: { items: [1] } },
        invoicesResult: { status: "degraded", data: { items: [1, 2] } },
        loyaltyOverviewResult: { status: "not-found" },
      },
      loyaltyBusinessesResult: { status: "ok", data: { items: [1] } },
    }).invoiceCount,
    2,
  );

  assert.equal(
    summarizeMemberEditorHealth({
      storefrontContext,
      identityContext: {
        profileResult: { status: "ok" },
        preferencesResult: { status: "degraded" },
        customerContextResult: { status: "ok" },
        addressesResult: { status: "ok", data: [] },
      },
    }).preferencesStatus,
    "degraded",
  );

  assert.equal(
    summarizeMemberCollectionHealth({
      storefrontContext,
      ordersResult: { status: "ok", data: { items: [1, 2] } },
    }).orderCount,
    2,
  );

  assert.equal(
    summarizeHomeDiscoveryHealth({
      storefrontContext,
      pagesResult: { status: "ok", data: { items: [1] } },
      categoriesResult: { status: "degraded", data: { items: [1, 2] } },
      productsResult: { status: "ok", data: { items: [1, 2, 3] } },
      categorySpotlights: [{ status: "ok" }, { status: "degraded" }],
    }).degradedSpotlightCount,
    1,
  );

  assert.deepEqual(
    summarizeHomeCategorySpotlightsHealth([
      {
        categorySlug: "featured",
        categoryProductsResult: { status: "ok", data: { items: [1] } },
      },
      {
        categorySlug: "seasonal",
        categoryProductsResult: { status: "degraded", data: { items: [] } },
      },
    ]),
    {
      spotlightCount: 2,
      degradedSpotlightCount: 1,
      spotlightProductCount: 1,
    },
  );

  assert.deepEqual(
    summarizeAccountPageHealth({
      session: null,
      publicRouteContext: {
        storefrontContext,
      },
    }),
    {
      sessionState: "missing",
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 1,
      cartStatus: "not-found",
      cartLinkedCount: 1,
    },
  );

  assert.equal(
    summarizeAccountPageHealth({
      session: { email: "member@example.com" },
      memberRouteContext: {
        storefrontContext,
        identityContext: {
          profileResult: { status: "ok" },
          preferencesResult: { status: "ok" },
          customerContextResult: { status: "degraded" },
          addressesResult: { status: "ok", data: [{ id: 1 }] },
        },
        commerceSummaryContext: {
          ordersResult: { status: "ok", data: { items: [1] } },
          invoicesResult: { status: "degraded", data: { items: [1, 2] } },
          loyaltyOverviewResult: { status: "not-found" },
        },
        loyaltyBusinessesResult: { status: "ok", data: { items: [1, 2] } },
      },
    }).sessionState,
    "present",
  );

  assert.deepEqual(
    summarizeHomeRouteHealth({
      memberSession: { email: "member@example.com" },
      parts: [1, 2, 3],
    }),
    {
      memberSessionState: "present",
      partCount: 3,
    },
  );

  assert.deepEqual(
    summarizeProductDetailRelatedHealth({
      status: "ok",
      data: { items: [1, 2, 3] },
    }),
    {
      status: "ok",
      relatedCount: 3,
    },
  );

  assert.equal(
    summarizeCommerceRouteHealth({
      storefrontContext,
      model: {
        anonymousId: "cart-1",
        status: "ok",
        cart: {
          items: [{ id: 1 }, { id: 2 }],
          currency: "EUR",
          grandTotalGrossMinor: 1200,
          couponCode: "SAVE10",
        },
      },
      memberSession: { email: "member@example.com" },
      identityContext: {
        profileResult: { status: "ok" },
        preferencesResult: { status: "ok" },
        customerContextResult: { status: "ok" },
        addressesResult: { status: "degraded", data: [] },
      },
      commerceSummaryContext: {
        ordersResult: { status: "ok", data: { items: [1, 2] } },
        invoicesResult: { status: "degraded", data: { items: [1] } },
        loyaltyOverviewResult: { status: "ok" },
      },
      confirmationResult: {
        status: "ok",
        data: {
          lines: [{ id: 1 }],
          payments: [{ status: "Paid" }, { status: "Pending" }],
        },
      },
    }).recordedPaidPaymentCount,
    1,
  );

  assert.deepEqual(
    summarizeConfirmationResultHealth({
      status: "ok",
      data: {
        lines: [{ id: 1 }],
        payments: [{ status: "Paid" }, { status: "Pending" }],
      },
    }),
    {
      status: "ok",
      lineCount: 1,
      paymentCount: 2,
      paidPaymentCount: 1,
    },
  );

  assert.deepEqual(
    summarizeCartPageHealth({
      routeContext: {
        model: {
          anonymousId: "cart-1",
          status: "ok",
          cart: {
            items: [{ id: 1 }, { id: 2 }],
            currency: "EUR",
            grandTotalGrossMinor: 1200,
          },
        },
        memberSession: { email: "member@example.com" },
        identityContext: {
          profileResult: { status: "ok" },
          preferencesResult: { status: "ok" },
          customerContextResult: { status: "ok" },
          addressesResult: { status: "ok", data: [{ id: 1 }] },
        },
      },
      followUpProducts: [{ id: "prod-1" }],
    }),
    {
      cartStatus: "ok",
      cartItemCount: 2,
      memberSessionState: "present",
      addressesStatus: "ok",
      followUpProductCount: 1,
    },
  );

  assert.deepEqual(
    summarizeCheckoutPageHealth({
      routeContext: {
        model: {
          anonymousId: "cart-1",
          status: "ok",
          cart: {
            items: [{ id: 1 }],
            currency: "EUR",
            grandTotalGrossMinor: 700,
          },
        },
        memberSession: null,
        identityContext: null,
        commerceSummaryContext: null,
      },
    }),
    {
      cartStatus: "ok",
      cartItemCount: 1,
      memberSessionState: "missing",
      addressesStatus: "unauthenticated",
      invoicesStatus: "unauthenticated",
      invoiceCount: 0,
    },
  );

  assert.deepEqual(
    summarizeConfirmationPageHealth({
      routeContext: {
        confirmationResult: {
          status: "ok",
          data: {
            lines: [{ id: 1 }, { id: 2 }],
          },
        },
        memberSession: { email: "member@example.com" },
        commerceSummaryContext: {
          ordersResult: { status: "ok", data: { items: [1] } },
          invoicesResult: { status: "degraded", data: { items: [1, 2] } },
          loyaltyOverviewResult: { status: "ok" },
        },
      },
      followUpProducts: [{ id: "prod-1" }, { id: "prod-2" }],
    }),
    {
      confirmationStatus: "ok",
      lineCount: 2,
      memberSessionState: "present",
      ordersStatus: "ok",
      invoicesStatus: "degraded",
      followUpProductCount: 2,
    },
  );

  assert.equal(
    summarizeStorefrontShoppingHealth({
      anonymousCartId: "cart-1",
      cartResult: { status: "ok", data: { items: [1, 2, 3] } },
      cartSnapshots: [{ id: 1 }],
      cartLinkedProductSlugs: ["prod"],
    }).liveCartItemCount,
    3,
  );

  assert.deepEqual(
    summarizeCartViewModelHealth({
      anonymousId: "cart-1",
      status: "ok",
      cart: {
        items: [{ id: 1 }, { id: 2 }],
        couponCode: "SAVE10",
      },
    }),
    {
      anonymousCartState: "present",
      cartStatus: "ok",
      cartItemCount: 2,
      hasCoupon: true,
    },
  );

  assert.equal(
    summarizeMemberIdentityHealth({
      profileResult: { status: "ok" },
      preferencesResult: { status: "degraded" },
      customerContextResult: { status: "ok" },
      addressesResult: { status: "ok", data: [{ id: 1 }] },
    }).preferencesStatus,
    "degraded",
  );

  assert.deepEqual(
    summarizePublicAuthRouteHealth({
      storefrontContext,
    }),
    {
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 1,
      cartStatus: "not-found",
      cartLinkedCount: 1,
    },
  );

  assert.deepEqual(
    summarizeProtectedMemberEntryHealth({
      session: null,
      storefrontContext,
    }),
    {
      sessionState: "missing",
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 1,
      cartStatus: "not-found",
      cartLinkedCount: 1,
    },
  );

  assert.deepEqual(
    summarizeProtectedMemberEntryHealth({
      session: { email: "member@example.com" },
      storefrontContext: null,
    }),
    {
      sessionState: "present",
    },
  );

  assert.equal(
    summarizeMemberCommerceSummaryHealth({
      ordersResult: { status: "ok", data: { items: [1, 2] } },
      invoicesResult: { status: "degraded", data: { items: [1] } },
      loyaltyOverviewResult: { status: "ok" },
    }).invoiceCount,
    1,
  );

  assert.deepEqual(
    summarizeMemberPagedCollectionHealth({
      status: "ok",
      data: {
        items: [1, 2, 3],
        total: 8,
        request: {
          page: 2,
          pageSize: 3,
        },
      },
    }),
    {
      status: "ok",
      itemCount: 3,
      totalCount: 8,
      page: 2,
      pageSize: 3,
    },
  );

  assert.deepEqual(
    summarizeMemberDetailHealth({
      status: "degraded",
      data: { id: "detail-1" },
    }),
    {
      status: "degraded",
      hasData: true,
    },
  );
});

test("localized discovery health helpers summarize alternates and sitemap inventory", () => {
  assert.deepEqual(
    summarizeLocalizedAlternatesMapHealth(
      new Map([
        ["cms-1", { "de-DE": "/cms/eins", "en-US": "/en-US/cms/one" }],
        ["cms-2", { "de-DE": "/cms/zwei" }],
      ]),
    ),
    {
      itemCount: 2,
      alternateCount: 3,
      multiCultureItemCount: 1,
    },
  );

  assert.deepEqual(
    summarizePublicSitemapHealth({
      entries: [1, 2, 3, 4, 5],
      staticEntryCount: 3,
      cmsEntryCount: 1,
      productEntryCount: 1,
    }),
    {
      totalEntryCount: 5,
      staticEntryCount: 3,
      cmsEntryCount: 1,
      productEntryCount: 1,
    },
  );

  assert.deepEqual(
    summarizeSeoMetadataHealth({
      canonicalPath: "/catalog/prod-1",
      noIndex: false,
      languageAlternates: {
        "de-DE": "/catalog/prod-1",
        "en-US": "/en-US/catalog/prod-1",
      },
    }),
    {
      canonicalPath: "/catalog/prod-1",
      noIndex: false,
      languageAlternateCount: 2,
    },
  );
});
