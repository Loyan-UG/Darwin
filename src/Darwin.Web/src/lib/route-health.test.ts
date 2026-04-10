import assert from "node:assert/strict";
import test from "node:test";
import {
  getLanguageAlternateState,
  getSeoIndexability,
  getSeoMetadataState,
  summarizeCommerceRouteHealth,
  summarizeCatalogRouteHealth,
  summarizeAccountPageHealth,
  summarizeCatalogBrowseCoreHealth,
  summarizeCatalogDetailCoreHealth,
  summarizeCatalogIndexPageHealth,
  summarizeCartPageHealth,
  summarizeCartViewModelHealth,
  summarizeCmsRouteHealth,
  summarizeCmsBrowseCoreHealth,
  summarizeCmsDetailCoreHealth,
  summarizeCmsIndexPageHealth,
  summarizeCheckoutPageHealth,
  summarizeConfirmationResultHealth,
  summarizeConfirmationPageHealth,
  summarizeHomeDiscoveryHealth,
  summarizeHomeRouteHealth,
  summarizeHomeCategorySpotlightsHealth,
  summarizeLocalizedAlternatesMapHealth,
  summarizeLocalizedDiscoveryInventoryHealth,
  summarizeLocalizedInventoryHealth,
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
  summarizeStorefrontSupportFootprint,
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
    products: [
      { id: "pr1", slug: "prod", name: "Prod", shortDescription: null, priceMinor: 100, compareAtPriceMinor: 150, currency: "EUR", primaryImageUrl: null },
      { id: "pr2", slug: "prod-2", name: "Prod 2", shortDescription: null, priceMinor: 200, compareAtPriceMinor: 220, currency: "EUR", primaryImageUrl: null },
      { id: "pr3", slug: "prod-3", name: "Prod 3", shortDescription: null, priceMinor: 300, compareAtPriceMinor: null, currency: "EUR", primaryImageUrl: null },
    ],
    productsStatus: "ok",
    storefrontCart: null,
    storefrontCartStatus: "not-found",
    cartSnapshots: [],
    cartLinkedProductSlugs: ["prod"],
  };
}

test("summarizeStorefrontSupportFootprint compacts storefront support state", () => {
  assert.equal(
    summarizeStorefrontSupportFootprint({
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      cartStatus: "not-found",
    }),
    "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",
  );
});
test("summarizePublicStorefrontHealth exposes canonical storefront statuses and counts", () => {
  assert.deepEqual(summarizePublicStorefrontHealth(createStorefrontContext()), {
    cmsStatus: "ok",
    cmsCount: 1,
    categoriesStatus: "degraded",
    categoryCount: 1,
    productsStatus: "ok",
    productCount: 3,
    heroOfferCount: 1,
    valueOfferCount: 2,
    liveOfferCount: 2,
    baseAssortmentCount: 1,
    promotionLaneFootprint: "hero:1|value:2|live:2|base:1",
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
      products: [
        { id: "base-1", slug: "base-1", name: "Base 1", shortDescription: null, priceMinor: 100, compareAtPriceMinor: null, currency: "EUR", primaryImageUrl: null },
        { id: "base-2", slug: "base-2", name: "Base 2", shortDescription: null, priceMinor: 120, compareAtPriceMinor: null, currency: "EUR", primaryImageUrl: null },
        { id: "base-3", slug: "base-3", name: "Base 3", shortDescription: null, priceMinor: 140, compareAtPriceMinor: null, currency: "EUR", primaryImageUrl: null },
      ],
    }),
    {
      cmsStatus: "ok",
      cmsCount: 2,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 0,
      valueOfferCount: 0,
      liveOfferCount: 0,
      baseAssortmentCount: 3,
      promotionLaneFootprint: "hero:0|value:0|live:0|base:3",
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
      shellMenuFootprint: "status:ok|items:3",
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
      shellModelFootprint: "culture:de-DE|source:cms|menu:ok|primary:2|utility:1|footer:3",
    },
  );
});

test("route health SEO helpers classify visibility and alternates directly", () => {
  assert.equal(getSeoIndexability(true), "noindex");
  assert.equal(getSeoIndexability(false), "indexable");

  assert.equal(getSeoMetadataState(true, 0), "private");
  assert.equal(getSeoMetadataState(false, 2), "localized");
  assert.equal(getSeoMetadataState(false, 0), "single-locale");

  assert.equal(getLanguageAlternateState(2), "present");
  assert.equal(getLanguageAlternateState(0), "missing");
});

test("route health helpers carry core and storefront health together", () => {
  const storefrontContext = createStorefrontContext();

  assert.deepEqual(
    summarizeCatalogRouteHealth({
      storefrontContext,
      browseContext: {
        categoriesResult: { status: "ok", data: { items: [1, 2] } },
        productsResult: { status: "degraded", data: { items: [1] } },
      },
    }),
    {
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:2|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      coreCategoriesStatus: "ok",
      coreCategoryCount: 2,
      coreProductsStatus: "degraded",
      coreProductCount: 1,
    },
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
      catalogDetailWorkflowFootprint: "product:ok:present|categories:ok:2",
    },
  );  assert.deepEqual(
    summarizeCatalogRouteHealth({
      storefrontContext,
      detailContext: {
        categoriesResult: { status: "ok", data: { items: [1, 2] } },
        productResult: { status: "ok", data: { id: "prod-1" } },
      },
    }),
    {
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:2|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      coreCategoriesStatus: "ok",
      coreCategoryCount: 2,
      coreProductsStatus: "ok",
      coreProductCount: 1,
    },
  );


  assert.deepEqual(
    summarizeCmsRouteHealth({
      storefrontContext,
      detailContext: {
        pageResult: { status: "ok", data: { id: "page-1" } },
        relatedPagesResult: { status: "ok", data: { items: [1, 2, 3] } },
      },
    }),
    {
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:2|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      corePagesStatus: "ok",
      corePageCount: 3,
      cmsWorkflowFootprint: "core:ok:3|lanes:hero:1|value:2|live:2|base:1",
      cmsSupportWorkflowFootprint: "categories:degraded:1|products:ok:3|cart:not-found",
    },
  );

  assert.deepEqual(
    summarizeCmsRouteHealth({
      storefrontContext,
      browseContext: {
        pagesResult: { status: "degraded", data: { items: [1, 2] } },
      },
    }),
    {
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:2|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      corePagesStatus: "degraded",
      corePageCount: 2,
      cmsWorkflowFootprint: "core:degraded:2|lanes:hero:1|value:2|live:2|base:1",
      cmsSupportWorkflowFootprint: "categories:degraded:1|products:ok:3|cart:not-found",
    },
  );

  assert.deepEqual(
    summarizeCmsBrowseCoreHealth({
      pagesResult: { status: "ok", data: { items: [1, 2, 3] } },
    }),
    {
      pagesStatus: "ok",
      pageCount: 3,
      cmsBrowseCoreFootprint: "pages:ok:3",
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
      cmsDetailWorkflowFootprint: "page:ok:present|related:degraded:2",
    },
  );
});

test("catalog and CMS index page health summaries expose direct browse and merchandising context", () => {
  const storefrontContext = createStorefrontContext();

  assert.deepEqual(
    summarizeCatalogIndexPageHealth({
      storefrontContext,
      browseContext: {
        categoriesResult: { status: "ok", data: { items: [1, 2] } },
        productsResult: { status: "ok", data: { items: [1, 2, 3] } },
      },
      visibleWindow: { items: [1, 2], total: 5 },
      matchingSetResult: { status: "ok" },
      matchingProductsTotal: 5,
      facetSummary: {
        offerCount: 2,
        baseCount: 3,
        withImageCount: 4,
        missingImageCount: 1,
        heroOfferCount: 1,
        valueOfferCount: 2,
      },
      hasBrowseLens: true,
    }),
    {
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 3,
      promotionLaneFootprint: "hero:1|value:2|live:2|base:3",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      coreCategoriesStatus: "ok",
      coreCategoryCount: 2,
      coreProductsStatus: "ok",
      coreProductCount: 3,
      browseMode: "windowed",
      visibleCount: 2,
      visibleTotal: 5,
      matchingStatus: "ok",
      matchingProductsTotal: 5,
      offerCount: 2,
      baseCount: 3,
      withImageCount: 4,
      missingImageCount: 1,
      catalogBrowseWorkflowFootprint: "mode:windowed|matching:ok:5|visible:2/5|lanes:hero:1|value:2|live:2|base:3",
      catalogSupportWorkflowFootprint: "cms:ok:1|products:ok:3|cart:not-found",
    },
  );

  assert.deepEqual(
    summarizeCmsIndexPageHealth({
      storefrontContext,
      browseContext: {
        pagesResult: { status: "ok", data: { items: [1, 2, 3] } },
      },
      visibleWindow: { items: [1, 2], total: 6 },
      matchingSetResult: { status: "ok" },
      matchingItemsTotal: 6,
      metadataSummary: {
        readyCount: 2,
        attentionCount: 4,
        missingMetaTitleCount: 1,
        missingMetaDescriptionCount: 2,
        missingBothCount: 1,
      },
      hasBrowseLens: true,
    }),
    {
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:2|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      corePagesStatus: "ok",
      corePageCount: 3,
      browseMode: "windowed",
      visibleCount: 2,
      visibleTotal: 6,
      matchingStatus: "ok",
      matchingItemsTotal: 6,
      readyCount: 2,
      attentionCount: 4,
      missingMetaTitleCount: 1,
      missingMetaDescriptionCount: 2,
      missingBothCount: 1,
      cmsBrowseWorkflowFootprint: "mode:windowed|matching:ok:6|visible:2/6|review:4|ready:2",
      cmsSupportWorkflowFootprint: "categories:degraded:1|products:ok:3|cart:not-found",
    },
  );
});
test("member and home health helpers summarize route readiness for diagnostics", () => {
  const storefrontContext = createStorefrontContext();

  assert.deepEqual(
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
    }),
    {
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:2|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      profileStatus: "ok",
      preferencesStatus: "ok",
      customerContextStatus: "degraded",
      addressesStatus: "ok",
      addressCount: 1,
      ordersStatus: "ok",
      orderCount: 1,
      invoicesStatus: "degraded",
      invoiceCount: 2,
      loyaltyStatus: "not-found",
      loyaltyBusinessesStatus: "ok",
      loyaltyBusinessCount: 1,
      memberWorkflowFootprint: "orders:ok|invoices:degraded|loyalty:not-found|lanes:hero:1|value:2|live:2|base:1",
      memberStorefrontSupportFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",
    },
  );

  assert.deepEqual(
    summarizeMemberEditorHealth({
      storefrontContext,
      identityContext: {
        profileResult: { status: "ok" },
        preferencesResult: { status: "degraded" },
        customerContextResult: { status: "ok" },
        addressesResult: { status: "ok", data: [] },
      },
    }),
    {
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:2|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      profileStatus: "ok",
      preferencesStatus: "degraded",
      customerContextStatus: "ok",
      addressesStatus: "ok",
      addressCount: 0,
      memberWorkflowFootprint: "profile:ok|preferences:degraded|addresses:0|lanes:hero:1|value:2|live:2|base:1",
      memberStorefrontSupportFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",
    },
  );

  assert.deepEqual(
    summarizeMemberCollectionHealth({
      storefrontContext,
      ordersResult: { status: "ok", data: { items: [1, 2] } },
    }),
    {
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:2|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      ordersStatus: "ok",
      orderCount: 2,
      invoicesStatus: "unknown",
      invoiceCount: 0,
      memberWorkflowFootprint: "orders:ok:2|invoices:unknown:0|lanes:hero:1|value:2|live:2|base:1",
      memberStorefrontSupportFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",
    },
  );

  assert.deepEqual(
    summarizeHomeDiscoveryHealth({
      storefrontContext,
      pagesResult: { status: "ok", data: { items: [1] } },
      categoriesResult: { status: "degraded", data: { items: [1, 2] } },
      productsResult: { status: "ok", data: { items: [1, 2, 3] } },
      categorySpotlights: [{ status: "ok" }, { status: "degraded" }],
    }),
    {
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:2|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      homePagesStatus: "ok",
      homePageCount: 1,
      homeCategoriesStatus: "degraded",
      homeCategoryCount: 2,
      homeProductsStatus: "ok",
      homeProductCount: 3,
      spotlightCount: 2,
      degradedSpotlightCount: 1,
      homeDiscoveryWorkflowFootprint: "pages:ok:1|categories:degraded:2|products:ok:3|spotlights:2|degraded:1",
    },
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
      homeCategorySpotlightsFootprint: "spotlights:2|degraded:1|products:1",
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
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:2|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      accountWorkflowFootprint: "surface:public|cart:not-found|lanes:hero:1|value:2|live:2|base:1",
      accountStorefrontSupportFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",    },
  );

  assert.deepEqual(
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
    }),
    {
      sessionState: "present",
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:2|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      profileStatus: "ok",
      preferencesStatus: "ok",
      customerContextStatus: "degraded",
      addressesStatus: "ok",
      addressCount: 1,
      ordersStatus: "ok",
      orderCount: 1,
      invoicesStatus: "degraded",
      invoiceCount: 2,
      loyaltyStatus: "not-found",
      loyaltyBusinessStatus: "ok",
      loyaltyBusinessCount: 2,
      memberWorkflowFootprint: "orders:ok|invoices:degraded|loyalty:not-found|lanes:hero:1|value:2|live:2|base:1",
      memberStorefrontSupportFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",
      accountWorkflowFootprint: "surface:member|orders:ok|invoices:degraded|lanes:hero:1|value:2|live:2|base:1",
      accountStorefrontSupportFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",    },
  );

  assert.deepEqual(
    summarizeHomeRouteHealth({
      memberSession: { email: "member@example.com" },
      homeDiscoveryContext: {
        storefrontContext,
      },
      parts: [1, 2, 3],
    }),
    {
      memberSessionState: "present",
      partCount: 3,
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:2|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      homeWorkflowFootprint: "session:present|parts:3|cart:not-found|lanes:hero:1|value:2|live:2|base:1",
      homeStorefrontSupportFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",    },
  );

  assert.deepEqual(
    summarizeProductDetailRelatedHealth({
      status: "ok",
      data: { items: [1, 2, 3] },
    }),
    {
      status: "ok",
      relatedCount: 3,
      productFollowUpFootprint: "related:ok:3",
    },
  );

  assert.deepEqual(
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
    }),
    {
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:2|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      cartModelStatus: "ok",
      cartItemCount: 2,
      hasAnonymousCart: true,
      hasCoupon: true,
      memberSessionState: "present",
      profileStatus: "ok",
      preferencesStatus: "ok",
      addressesStatus: "degraded",
      invoiceSummaryStatus: "degraded",
      invoiceSummaryCount: 1,
      orderSummaryStatus: "ok",
      orderSummaryCount: 2,
      confirmationStatus: "ok",
      confirmationLineCount: 1,
      confirmationPaymentCount: 2,
      recordedPaidPaymentCount: 1,
      commerceRouteWorkflowFootprint: "session:present|cart:ok|addresses:degraded|orders:ok|invoices:degraded|confirmation:ok|lanes:hero:1|value:2|live:2|base:1",
      commerceRouteStorefrontSupportFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",    },
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
      confirmationWorkflowFootprint: "status:ok|lines:1|payments:2|paid:1",
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
      followUpProducts: [
        { id: "hero-1", slug: "hero-1", name: "Hero 1", shortDescription: null, priceMinor: 100, compareAtPriceMinor: 150, currency: "EUR", primaryImageUrl: null },
        { id: "base-1", slug: "base-1", name: "Base 1", shortDescription: null, priceMinor: 120, compareAtPriceMinor: null, currency: "EUR", primaryImageUrl: null },
      ],
    }),
    {
      cartStatus: "ok",
      cartItemCount: 2,
      memberSessionState: "present",
      addressesStatus: "ok",
      followUpProductCount: 2,
      heroOfferCount: 1,
      valueOfferCount: 1,
      liveOfferCount: 1,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:1|base:1",
      commerceWorkflowFootprint: "surface:cart|session:present|addresses:ok|lanes:hero:1|value:1|live:1|base:1",
      commerceStorefrontSupportFootprint: "products:2|cart:ok|addresses:ok",    },
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
        storefrontContext: createStorefrontContext(),
      },
    }),
    {
      cartStatus: "ok",
      cartItemCount: 1,
      memberSessionState: "missing",
      addressesStatus: "unauthenticated",
      invoicesStatus: "unauthenticated",
      invoiceCount: 0,
      heroOfferCount: 1,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:2|live:2|base:1",
      commerceWorkflowFootprint: "surface:checkout|session:missing|addresses:unauthenticated|invoices:unauthenticated|lanes:hero:1|value:2|live:2|base:1",
      commerceStorefrontSupportFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",    },
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
      followUpProducts: [
        { id: "value-1", slug: "value-1", name: "Value 1", shortDescription: null, priceMinor: 100, compareAtPriceMinor: 130, currency: "EUR", primaryImageUrl: null },
        { id: "base-1", slug: "base-1", name: "Base 1", shortDescription: null, priceMinor: 180, compareAtPriceMinor: null, currency: "EUR", primaryImageUrl: null },
      ],
    }),
    {
      confirmationStatus: "ok",
      lineCount: 2,
      memberSessionState: "present",
      ordersStatus: "ok",
      invoicesStatus: "degraded",
      followUpProductCount: 2,
      heroOfferCount: 0,
      valueOfferCount: 1,
      liveOfferCount: 1,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:0|value:1|live:1|base:1",
      commerceWorkflowFootprint: "surface:confirmation|session:present|orders:ok|invoices:degraded|lanes:hero:0|value:1|live:1|base:1",
      confirmationFollowUpWorkflowFootprint: "products:2|lanes:hero:0|value:1|live:1|base:1",
      commerceStorefrontSupportFootprint: "products:2|orders:ok|invoices:degraded",    },
  );

  assert.deepEqual(
    summarizeStorefrontShoppingHealth({
      anonymousCartId: "cart-1",
      cartResult: { status: "ok", data: { items: [1, 2, 3] } },
      cartSnapshots: [{ id: 1 }],
      cartLinkedProductSlugs: ["prod"],
    }),
    {
      anonymousCartState: "present",
      liveCartStatus: "ok",
      liveCartItemCount: 3,
      snapshotCount: 1,
      cartLinkedCount: 1,
      storefrontShoppingFootprint: "anonymous:present|live:ok:3|snapshots:1|linked:1",
    },
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
      cartModelFootprint: "anonymous:present|status:ok|items:2|coupon:yes",
    },
  );

  assert.deepEqual(
    summarizeMemberIdentityHealth({
      profileResult: { status: "ok" },
      preferencesResult: { status: "degraded" },
      customerContextResult: { status: "ok" },
      addressesResult: { status: "ok", data: [{ id: 1 }] },
    }),
    {
      profileStatus: "ok",
      preferencesStatus: "degraded",
      customerContextStatus: "ok",
      addressesStatus: "ok",
      addressCount: 1,
      memberIdentityFootprint: "profile:ok|preferences:degraded|customer:ok|addresses:ok:1",
    },
  );

  assert.deepEqual(
    summarizePublicAuthRouteHealth({
      storefrontContext,
      route: "/account/sign-in",
    }),
    {
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:2|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      authEntryWorkflowFootprint: "route:/account/sign-in|cart:not-found|lanes:hero:1|value:2|live:2|base:1",
      authEntryStorefrontSupportFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",    },
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
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:2|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      memberEntryWorkflowFootprint: "session:missing|cart:not-found|lanes:hero:1|value:2|live:2|base:1",
      memberEntryStorefrontSupportFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",    },
  );

  assert.deepEqual(
    summarizeProtectedMemberEntryHealth({
      session: { email: "member@example.com" },
      storefrontContext: null,
    }),
    {
      sessionState: "present",
      memberEntryWorkflowFootprint: "session:present|storefront:missing",
      memberEntryStorefrontSupportFootprint: "storefront:missing",    },
  );

  assert.deepEqual(
    summarizeMemberCommerceSummaryHealth({
      ordersResult: { status: "ok", data: { items: [1, 2] } },
      invoicesResult: { status: "degraded", data: { items: [1] } },
      loyaltyOverviewResult: { status: "ok" },
    }),
    {
      ordersStatus: "ok",
      orderCount: 2,
      invoicesStatus: "degraded",
      invoiceCount: 1,
      loyaltyStatus: "ok",
      memberCommerceSummaryFootprint: "orders:ok:2|invoices:degraded:1|loyalty:ok",
    },
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
      memberCollectionWorkflowFootprint: "status:ok|page:2|size:3|items:3|total:8",
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
      memberWorkflowFootprint: "detail:degraded|has-data:yes",
    },
  );
});

test("localized discovery health helpers summarize alternates and sitemap inventory", () => {
  assert.deepEqual(
    summarizeLocalizedInventoryHealth([
      { culture: "de-DE", items: [1, 2] },
      { culture: "en-US", items: [] },
    ]),
    {
      localizedDiscoveryState: "present",
      localizedCultureCount: 2,
      localizedItemCount: 2,
      emptyCultureCount: 1,
      localizedDiscoveryDetailFootprint: "cultures:2|items:2|empty:1",
      localizedInventoryFootprint: "cultures:2|items:2|empty:1",
      localizedInventorySummaryFootprint: "cultures:2|items:2|empty:1",
    },
  );

  assert.deepEqual(
    summarizeLocalizedAlternatesMapHealth(
      new Map([
        ["cms-1", { "de-DE": "/cms/eins", "en-US": "/en-US/cms/one" }],
        ["cms-2", { "de-DE": "/cms/zwei" }],
      ]),
    ),
    {
      localizedDiscoveryState: "present",
      itemCount: 2,
      alternateCount: 3,
      multiCultureItemCount: 1,
      localizedDiscoveryDetailFootprint: "items:2|alternates:3|multi:1",
      alternateMapFootprint: "items:2|alternates:3|multi:1",
      alternateMapSummaryFootprint: "items:2|alternates:3|multi:1",
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
      localizedDiscoveryState: "present",
      totalEntryCount: 5,
      staticEntryCount: 3,
      cmsEntryCount: 1,
      productEntryCount: 1,
      localizedDiscoveryDetailFootprint: "static:3|cms:1|products:1",
      sitemapCompositionFootprint: "static:3|cms:1|products:1",
      sitemapSummaryFootprint: "total:5|static:3|cms:1|products:1",
    },
  );

  assert.deepEqual(
    summarizeLocalizedDiscoveryInventoryHealth({
      pages: [
        { culture: "de-DE", items: [1, 2] },
        { culture: "en-US", items: [] },
      ],
      products: [
        { culture: "de-DE", items: [1] },
        { culture: "en-US", items: [1, 2, 3] },
      ],
    }),
    {
      localizedDiscoveryState: "present",
      localizedCultureCount: 2,
      localizedPageCount: 2,
      localizedProductCount: 4,
      emptyPageCultureCount: 1,
      emptyProductCultureCount: 0,
      localizedDiscoveryDetailFootprint: "pages-empty:1|products-empty:0",
      localizedDiscoveryFootprint: "pages-empty:1|products-empty:0",
      localizedDiscoverySummaryFootprint:
        "cultures:2|pages:2|products:4|pages-empty:1|products-empty:0",
    },
  );

  assert.deepEqual(
    summarizeSeoMetadataHealth({
      canonicalPath: "/catalog/prod-1",
      noIndex: false,
      languageAlternates: {
        "en-US": "/en-US/catalog/prod-1",
        "x-default": "/catalog/prod-1",
        "de-DE": "/catalog/prod-1",
      },
    }),
    {
      canonicalPath: "/catalog/prod-1",
      noIndex: false,
      seoIndexability: "indexable",
      seoMetadataState: "localized",
      seoVisibilityFootprint: "indexable|localized",
      languageAlternateState: "present",
      languageAlternateCount: 3,
      languageAlternateFootprint: "x-default|de-DE|en-US",
      seoAlternateDetailFootprint: "x-default|de-DE|en-US",
      seoAlternateSummaryFootprint: "alternates:3[x-default|de-DE|en-US]",
      seoSummaryFootprint: "indexable|alternates:3[x-default|de-DE|en-US]",
      seoTargetFootprint: "indexable|/catalog/prod-1",
    },
  );

  assert.deepEqual(
    summarizeSeoMetadataHealth({
      canonicalPath: "/account/sign-in",
      noIndex: true,
    }),
    {
      canonicalPath: "/account/sign-in",
      noIndex: true,
      seoIndexability: "noindex",
      seoMetadataState: "private",
      seoVisibilityFootprint: "noindex|private",
      languageAlternateState: "missing",
      languageAlternateCount: 0,
      languageAlternateFootprint: "none",
      seoAlternateDetailFootprint: "none",
      seoAlternateSummaryFootprint: "alternates:none",
      seoSummaryFootprint: "noindex|alternates:0[none]",
      seoTargetFootprint: "noindex|/account/sign-in",
    },
  );

  assert.deepEqual(
    summarizeSeoMetadataHealth({
      canonicalPath: "/catalog",
      noIndex: false,
    }),
    {
      canonicalPath: "/catalog",
      noIndex: false,
      seoIndexability: "indexable",
      seoMetadataState: "single-locale",
      seoVisibilityFootprint: "indexable|single-locale",
      languageAlternateState: "missing",
      languageAlternateCount: 0,
      languageAlternateFootprint: "none",
      seoAlternateDetailFootprint: "none",
      seoAlternateSummaryFootprint: "alternates:none",
      seoSummaryFootprint: "indexable|alternates:0[none]",
      seoTargetFootprint: "indexable|/catalog",
    },
  );

  assert.deepEqual(summarizeLocalizedInventoryHealth([]), {
    localizedDiscoveryState: "empty",
    localizedCultureCount: 0,
    localizedItemCount: 0,
    emptyCultureCount: 0,
    localizedDiscoveryDetailFootprint: "cultures:0|items:0|empty:0",
    localizedInventoryFootprint: "cultures:0|items:0|empty:0",
    localizedInventorySummaryFootprint: "cultures:0|items:0|empty:0",
  });

  assert.deepEqual(summarizeLocalizedAlternatesMapHealth(new Map()), {
    localizedDiscoveryState: "empty",
    itemCount: 0,
    alternateCount: 0,
    multiCultureItemCount: 0,
    localizedDiscoveryDetailFootprint: "items:0|alternates:0|multi:0",
    alternateMapFootprint: "items:0|alternates:0|multi:0",
    alternateMapSummaryFootprint: "items:0|alternates:0|multi:0",
  });

  assert.deepEqual(
    summarizePublicSitemapHealth({
      entries: [],
      staticEntryCount: 0,
      cmsEntryCount: 0,
      productEntryCount: 0,
    }),
    {
      localizedDiscoveryState: "empty",
      totalEntryCount: 0,
      staticEntryCount: 0,
      cmsEntryCount: 0,
      productEntryCount: 0,
      localizedDiscoveryDetailFootprint: "static:0|cms:0|products:0",
      sitemapCompositionFootprint: "static:0|cms:0|products:0",
      sitemapSummaryFootprint: "total:0|static:0|cms:0|products:0",
    },
  );
});




































