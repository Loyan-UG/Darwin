import assert from "node:assert/strict";
import test from "node:test";
import {
  buildStorefrontHealthSummary,
  getLanguageAlternateState,
  getSeoIndexability,
  getSeoMetadataState,
  summarizePromotionLaneHealth,
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
  summarizeStorefrontContextFootprint,
} from "@/lib/route-health";
import type { PublicStorefrontContext } from "@/features/storefront/public-storefront-context";

function createProduct(
  slug: string,
  priceMinor: number,
  compareAtPriceMinor: number | null,
) {
  return {
    id: slug,
    slug,
    name: slug,
    shortDescription: null,
    priceMinor,
    compareAtPriceMinor,
    currency: "EUR",
    primaryImageUrl: null,
  };
}

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

test("summarizeStorefrontContextFootprint compacts storefront context state", () => {
  assert.equal(
    summarizeStorefrontContextFootprint({
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

  assert.equal(
    summarizeStorefrontContextFootprint({
      cmsStatus: "unknown",
      cmsCount: 0,
      categoriesStatus: "unknown",
      categoryCount: 0,
      productsStatus: "unknown",
      productCount: 0,
      cartStatus: "unknown",
    }),
    "cms:unknown:0|categories:unknown:0|products:unknown:0|cart:unknown",
  );
});

test("storefront helper builders keep merchandising and optional cart state explicit", () => {
  assert.deepEqual(
    summarizePromotionLaneHealth([
      createProduct("hero-1", 500, 900),
      createProduct("value-1", 400, 700),
      createProduct("base-1", 300, null),
    ]),
    {
      heroOfferCount: 2,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:2|value:2|live:2|base:1",
    },
  );

  assert.deepEqual(
    buildStorefrontHealthSummary({
      cmsStatus: "ok",
      cmsCount: 2,
      categoriesStatus: "fallback",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      products: [
        createProduct("hero-1", 500, 900),
        createProduct("value-1", 400, 700),
        createProduct("base-1", 300, null),
      ],
      cartStatus: "present",
      cartLinkedCount: 2,
    }),
    {
      cmsStatus: "ok",
      cmsCount: 2,
      categoriesStatus: "fallback",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 2,
      valueOfferCount: 2,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:2|value:2|live:2|base:1",
      cartStatus: "present",
      cartLinkedCount: 2,
    },
  );

  assert.deepEqual(
    buildStorefrontHealthSummary({
      cmsStatus: "unknown",
      cmsCount: 0,
      categoriesStatus: "unknown",
      categoryCount: 0,
      productsStatus: "unknown",
      productCount: 0,
      products: [],
    }),
    {
      cmsStatus: "unknown",
      cmsCount: 0,
      categoriesStatus: "unknown",
      categoryCount: 0,
      productsStatus: "unknown",
      productCount: 0,
      heroOfferCount: 0,
      valueOfferCount: 0,
      liveOfferCount: 0,
      baseAssortmentCount: 0,
      promotionLaneFootprint: "hero:0|value:0|live:0|base:0",
    },
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
    valueOfferCount: 1,
    liveOfferCount: 2,
    baseAssortmentCount: 1,
    promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
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

  const emptyStorefrontContext: PublicStorefrontContext = {
    cmsPagesResult: { data: null, status: "unknown" },
    cmsPages: [],
    cmsPagesStatus: "unknown",
    categoriesResult: { data: null, status: "unknown" },
    categories: [],
    categoriesStatus: "unknown",
    productsResult: { data: null, status: "unknown" },
    products: [],
    productsStatus: "unknown",
    storefrontCart: null,
    storefrontCartStatus: "not-found",
    cartSnapshots: [],
    cartLinkedProductSlugs: [],
  };

  assert.deepEqual(summarizePublicStorefrontHealth(emptyStorefrontContext), {
    cmsStatus: "unknown",
    cmsCount: 0,
    categoriesStatus: "unknown",
    categoryCount: 0,
    productsStatus: "unknown",
    productCount: 0,
    heroOfferCount: 0,
    valueOfferCount: 0,
    liveOfferCount: 0,
    baseAssortmentCount: 0,
    promotionLaneFootprint: "hero:0|value:0|live:0|base:0",
    cartStatus: "not-found",
    cartLinkedCount: 0,
  });

  assert.deepEqual(
    summarizeStorefrontContinuationHealth({
      cmsPagesStatus: "unknown",
      cmsPages: [],
      categoriesStatus: "unknown",
      categories: [],
      productsStatus: "unknown",
      products: [],
    }),
    {
      cmsStatus: "unknown",
      cmsCount: 0,
      categoriesStatus: "unknown",
      categoryCount: 0,
      productsStatus: "unknown",
      productCount: 0,
      heroOfferCount: 0,
      valueOfferCount: 0,
      liveOfferCount: 0,
      baseAssortmentCount: 0,
      promotionLaneFootprint: "hero:0|value:0|live:0|base:0",
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
    summarizeShellHealth({
      status: "network-error",
      data: null,
    }),
    {
      menuStatus: "network-error",
      menuItemCount: 0,
      shellMenuFootprint: "status:network-error|items:0",
    },
  );

  assert.deepEqual(
    summarizeShellHealth({
      status: "unknown",
      data: null,
    }),
    {
      menuStatus: "unknown",
      menuItemCount: 0,
      shellMenuFootprint: "status:unknown|items:0",
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

  assert.deepEqual(
    summarizeShellModelHealth({
      culture: "en-US",
      menuSource: "fallback",
      menuStatus: "network-error",
      primaryNavigation: [1, 2, 3, 4, 5, 6, 7, 8],
      utilityLinks: [1, 2, 3],
      footerGroups: [1, 2, 3],
    }),
    {
      culture: "en-US",
      menuSource: "fallback",
      menuStatus: "network-error",
      primaryNavigationCount: 8,
      utilityLinkCount: 3,
      footerGroupCount: 3,
      shellModelFootprint: "culture:en-US|source:fallback|menu:network-error|primary:8|utility:3|footer:3",
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
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
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
      catalogBrowseCoreFootprint: "categories:ok:2|products:degraded:1",
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
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
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
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      corePagesStatus: "ok",
      corePageCount: 3,
      cmsWorkflowFootprint: "core:ok:3|lanes:hero:1|value:1|live:2|base:1",
      cmsContextFootprint: "categories:degraded:1|products:ok:3|cart:not-found",
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
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      corePagesStatus: "degraded",
      corePageCount: 2,
      cmsWorkflowFootprint: "core:degraded:2|lanes:hero:1|value:1|live:2|base:1",
      cmsContextFootprint: "categories:degraded:1|products:ok:3|cart:not-found",
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

  assert.deepEqual(
    summarizeCatalogBrowseCoreHealth({
      categoriesResult: { status: "unknown", data: null },
      productsResult: { status: "unknown", data: null },
    }),
    {
      categoriesStatus: "unknown",
      categoryCount: 0,
      productsStatus: "unknown",
      productCount: 0,
      catalogBrowseCoreFootprint: "categories:unknown:0|products:unknown:0",
    },
  );

  assert.deepEqual(
    summarizeCatalogDetailCoreHealth({
      categoriesResult: { status: "unknown", data: null },
      productResult: { status: "unknown", data: null },
    }),
    {
      categoriesStatus: "unknown",
      categoryCount: 0,
      productStatus: "unknown",
      hasProduct: false,
      catalogDetailWorkflowFootprint: "product:unknown:missing|categories:unknown:0",
    },
  );

  assert.deepEqual(
    summarizeCmsBrowseCoreHealth({
      pagesResult: { status: "unknown", data: null },
    }),
    {
      pagesStatus: "unknown",
      pageCount: 0,
      cmsBrowseCoreFootprint: "pages:unknown:0",
    },
  );

  assert.deepEqual(
    summarizeCmsDetailCoreHealth({
      pageResult: { status: "unknown", data: null },
      relatedPagesResult: { status: "unknown", data: null },
    }),
    {
      pageStatus: "unknown",
      hasPage: false,
      relatedSeedStatus: "unknown",
      relatedSeedCount: 0,
      cmsDetailWorkflowFootprint: "page:unknown:missing|related:unknown:0",
    },
  );
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
      catalogContextFootprint: "cms:ok:1|products:ok:3|cart:not-found",
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
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      corePagesStatus: "ok",
      corePageCount: 3,
      cmsWorkflowFootprint: "core:ok:3|lanes:hero:1|value:1|live:2|base:1",
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
      cmsBrowseWorkflowFootprint: "mode:windowed|matching:ok:6|visible:2/6|attention:4|ready:2",
      cmsContextFootprint: "categories:degraded:1|products:ok:3|cart:not-found",
    },
  );
});
test("member and home health helpers summarize route state for diagnostics", () => {
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
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
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
      memberWorkflowFootprint: "orders:ok|invoices:degraded|loyalty:not-found|lanes:hero:1|value:1|live:2|base:1",
      memberStorefrontContextFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",
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
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      profileStatus: "ok",
      preferencesStatus: "degraded",
      customerContextStatus: "ok",
      addressesStatus: "ok",
      addressCount: 0,
      memberWorkflowFootprint: "profile:ok|preferences:degraded|addresses:0|lanes:hero:1|value:1|live:2|base:1",
      memberStorefrontContextFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",
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
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      ordersStatus: "ok",
      orderCount: 2,
      invoicesStatus: "unknown",
      invoiceCount: 0,
      memberWorkflowFootprint: "orders:ok:2|invoices:unknown:0|lanes:hero:1|value:1|live:2|base:1",
      memberStorefrontContextFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",
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
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
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
    summarizeHomeCategorySpotlightsHealth([]),
    {
      spotlightCount: 0,
      degradedSpotlightCount: 0,
      spotlightProductCount: 0,
      homeCategorySpotlightsFootprint: "spotlights:0|degraded:0|products:0",
    },
  );

  assert.deepEqual(
    summarizeCheckoutPageHealth({
      routeContext: {
        model: {
          anonymousId: null,
          status: "unknown",
          cart: null,
        },
        memberSession: null,
        identityContext: null,
        commerceSummaryContext: null,
        storefrontContext,
      },
    }),
    {
      cartStatus: "unknown",
      cartItemCount: 0,
      memberSessionState: "missing",
      addressesStatus: "unauthenticated",
      invoicesStatus: "unauthenticated",
      invoiceCount: 0,
      heroOfferCount: 1,
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
      commerceWorkflowFootprint: "surface:checkout|session:missing|addresses:unauthenticated|invoices:unauthenticated|lanes:hero:1|value:1|live:2|base:1",
      commerceStorefrontContextFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",
    },
  );

  assert.deepEqual(
    summarizeConfirmationPageHealth({
      routeContext: {
        confirmationResult: {
          status: "unknown",
          data: null,
        },
        memberSession: null,
        commerceSummaryContext: null,
      },
      followUpProducts: [],
    }),
    {
      confirmationStatus: "unknown",
      lineCount: 0,
      memberSessionState: "missing",
      ordersStatus: "unauthenticated",
      invoicesStatus: "unauthenticated",
      followUpProductCount: 0,
      heroOfferCount: 0,
      valueOfferCount: 0,
      liveOfferCount: 0,
      baseAssortmentCount: 0,
      promotionLaneFootprint: "hero:0|value:0|live:0|base:0",
      commerceWorkflowFootprint: "surface:confirmation|session:missing|orders:unauthenticated|invoices:unauthenticated|lanes:hero:0|value:0|live:0|base:0",
      confirmationFollowUpWorkflowFootprint: "products:0|lanes:hero:0|value:0|live:0|base:0",
      commerceStorefrontContextFootprint: "products:0|orders:unauthenticated|invoices:unauthenticated",
    },
  );
  assert.deepEqual(
    summarizeStorefrontShoppingHealth({
      anonymousCartId: null,
      cartResult: { status: "unknown", data: null },
      cartSnapshots: [],
      cartLinkedProductSlugs: [],
    }),
    {
      anonymousCartState: "missing",
      liveCartStatus: "unknown",
      liveCartItemCount: 0,
      snapshotCount: 0,
      cartLinkedCount: 0,
      storefrontShoppingFootprint: "anonymous:missing|live:unknown:0|snapshots:0|linked:0",
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
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      accountWorkflowFootprint: "surface:public|cart:not-found|lanes:hero:1|value:1|live:2|base:1",
      accountStorefrontContextFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",    },
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
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
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
      loyaltyBusinessCount: 2,
      memberWorkflowFootprint: "orders:ok|invoices:degraded|loyalty:not-found|lanes:hero:1|value:1|live:2|base:1",
      memberStorefrontContextFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",
      accountWorkflowFootprint: "surface:member|orders:ok|invoices:degraded|lanes:hero:1|value:1|live:2|base:1",
      accountStorefrontContextFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",    },
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
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      homeWorkflowFootprint: "session:present|parts:3|cart:not-found|lanes:hero:1|value:1|live:2|base:1",
      homeStorefrontContextFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",    },
  );

  assert.deepEqual(
    summarizeHomeRouteHealth({
      memberSession: null,
      homeDiscoveryContext: {
        storefrontContext,
      },
      parts: [],
    }),
    {
      memberSessionState: "missing",
      partCount: 0,
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      homeWorkflowFootprint: "session:missing|parts:0|cart:not-found|lanes:hero:1|value:1|live:2|base:1",
      homeStorefrontContextFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",
    },
  );

  assert.deepEqual(
    summarizeCartPageHealth({
      routeContext: {
        model: {
          anonymousId: null,
          status: "unknown",
          cart: null,
        },
        memberSession: null,
        identityContext: null,
      },
      followUpProducts: [],
    }),
    {
      cartStatus: "unknown",
      cartItemCount: 0,
      memberSessionState: "missing",
      addressesStatus: "unauthenticated",
      followUpProductCount: 0,
      heroOfferCount: 0,
      valueOfferCount: 0,
      liveOfferCount: 0,
      baseAssortmentCount: 0,
      promotionLaneFootprint: "hero:0|value:0|live:0|base:0",
      commerceWorkflowFootprint: "surface:cart|session:missing|addresses:unauthenticated|lanes:hero:0|value:0|live:0|base:0",
      commerceStorefrontContextFootprint: "products:0|cart:unknown|addresses:unauthenticated",
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
      productRelatedWorkflowFootprint: "status:ok|related:3",
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
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
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
    },
  );

  assert.deepEqual(
    summarizeProductDetailRelatedHealth({
      status: "unknown",
      data: null,
    }),
    {
      status: "unknown",
      relatedCount: 0,
      productRelatedWorkflowFootprint: "status:unknown|related:0",
    },
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
      commerceStorefrontContextFootprint: "products:2|cart:ok|addresses:ok",    },
  );

  assert.deepEqual(
    summarizeHomeDiscoveryHealth({
      storefrontContext,
      pagesResult: { status: "unknown", data: null },
      categoriesResult: { status: "unknown", data: null },
      productsResult: { status: "unknown", data: null },
      categorySpotlights: [],
    }),
    {
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      homePagesStatus: "unknown",
      homePageCount: 0,
      homeCategoriesStatus: "unknown",
      homeCategoryCount: 0,
      homeProductsStatus: "unknown",
      homeProductCount: 0,
      spotlightCount: 0,
      degradedSpotlightCount: 0,
      homeDiscoveryWorkflowFootprint: "pages:unknown:0|categories:unknown:0|products:unknown:0|spotlights:0|degraded:0",
    },
  );

  assert.deepEqual(
    summarizeCommerceRouteHealth({
      storefrontContext,
    }),
    {
      cmsStatus: "ok",
      cmsCount: 1,
      categoriesStatus: "degraded",
      categoryCount: 1,
      productsStatus: "ok",
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      cartModelStatus: "unknown",
      cartItemCount: 0,
      hasAnonymousCart: false,
      hasCoupon: false,
      memberSessionState: "missing",
      profileStatus: "unknown",
      preferencesStatus: "unknown",
      addressesStatus: "unknown",
      invoiceSummaryStatus: "unknown",
      invoiceSummaryCount: 0,
      orderSummaryStatus: "unknown",
      orderSummaryCount: 0,
      confirmationStatus: "unknown",
      confirmationLineCount: 0,
      confirmationPaymentCount: 0,
      recordedPaidPaymentCount: 0,
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
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
      commerceWorkflowFootprint: "surface:checkout|session:missing|addresses:unauthenticated|invoices:unauthenticated|lanes:hero:1|value:1|live:2|base:1",
      commerceStorefrontContextFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",    },
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
      commerceStorefrontContextFootprint: "products:2|orders:ok|invoices:degraded",    },
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
    summarizeConfirmationResultHealth({
      status: "unknown",
      data: null,
    }),
    {
      status: "unknown",
      lineCount: 0,
      paymentCount: 0,
      paidPaymentCount: 0,
      confirmationWorkflowFootprint: "status:unknown|lines:0|payments:0|paid:0",
    },
  );

  assert.deepEqual(
    summarizeCartViewModelHealth({
      anonymousId: null,
      status: "unknown",
      cart: null,
    }),
    {
      anonymousCartState: "missing",
      cartStatus: "unknown",
      cartItemCount: 0,
      hasCoupon: false,
      cartModelFootprint: "anonymous:missing|status:unknown|items:0|coupon:no",
    },
  );

  assert.deepEqual(
    summarizeShellModelHealth({
      culture: "en-US",
      menuSource: "fallback",
      menuStatus: "missing",
      primaryNavigation: [],
      utilityLinks: [],
      footerGroups: [],
    }),
    {
      culture: "en-US",
      menuSource: "fallback",
      menuStatus: "missing",
      primaryNavigationCount: 0,
      utilityLinkCount: 0,
      footerGroupCount: 0,
      shellModelFootprint: "culture:en-US|source:fallback|menu:missing|primary:0|utility:0|footer:0",
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
    summarizeMemberIdentityHealth({
      profileResult: { status: "unknown" },
      preferencesResult: { status: "unknown" },
      customerContextResult: { status: "unknown" },
      addressesResult: { status: "unknown", data: null },
    }),
    {
      profileStatus: "unknown",
      preferencesStatus: "unknown",
      customerContextStatus: "unknown",
      addressesStatus: "unknown",
      addressCount: 0,
      memberIdentityFootprint: "profile:unknown|preferences:unknown|customer:unknown|addresses:unknown:0",
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
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      authEntryWorkflowFootprint: "route:/account/sign-in|cart:not-found|lanes:hero:1|value:1|live:2|base:1",
      authEntryStorefrontContextFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",    },
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
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      memberEntryWorkflowFootprint: "session:missing|cart:not-found|lanes:hero:1|value:1|live:2|base:1",
      memberEntryStorefrontContextFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",    },
  );

  assert.deepEqual(
    summarizeProtectedMemberEntryHealth({
      session: { email: "member@example.com" },
      storefrontContext: null,
    }),
    {
      sessionState: "present",
      memberEntryWorkflowFootprint: "session:present|storefront:missing",
      memberEntryStorefrontContextFootprint: "storefront:missing",    },
  );

  assert.deepEqual(
    summarizeAccountPageHealth({
      session: null,
      publicRouteContext: null,
    }),
    {
      sessionState: "missing",
      accountWorkflowFootprint: "surface:public|storefront:missing",
      accountStorefrontContextFootprint: "storefront:missing",
    },
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
      productCount: 3,
      heroOfferCount: 1,
      valueOfferCount: 1,
      liveOfferCount: 2,
      baseAssortmentCount: 1,
      promotionLaneFootprint: "hero:1|value:1|live:2|base:1",
      cartStatus: "not-found",
      cartLinkedCount: 1,
      authEntryWorkflowFootprint: "route:unknown|cart:not-found|lanes:hero:1|value:1|live:2|base:1",
      authEntryStorefrontContextFootprint: "cms:ok:1|categories:degraded:1|products:ok:3|cart:not-found",
    },
  );

  assert.deepEqual(
    summarizeProtectedMemberEntryHealth({
      session: null,
      storefrontContext: null,
    }),
    {
      sessionState: "missing",
      memberEntryWorkflowFootprint: "session:missing|storefront:missing",
      memberEntryStorefrontContextFootprint: "storefront:missing",
    },
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
    summarizeMemberCommerceSummaryHealth({
      ordersResult: { status: "unknown", data: null },
      invoicesResult: { status: "unknown", data: null },
      loyaltyOverviewResult: { status: "unknown" },
    }),
    {
      ordersStatus: "unknown",
      orderCount: 0,
      invoicesStatus: "unknown",
      invoiceCount: 0,
      loyaltyStatus: "unknown",
      memberCommerceSummaryFootprint: "orders:unknown:0|invoices:unknown:0|loyalty:unknown",
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
        ["cms-1", { "de-DE": "/page/eins", "en-US": "/en-US/page/one" }],
        ["cms-2", { "de-DE": "/page/zwei" }],
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
    summarizeLocalizedDiscoveryInventoryHealth({
      pages: [],
      products: [],
    }),
    {
      localizedDiscoveryState: "empty",
      localizedCultureCount: 0,
      localizedPageCount: 0,
      localizedProductCount: 0,
      emptyPageCultureCount: 0,
      emptyProductCultureCount: 0,
      localizedDiscoveryDetailFootprint: "pages-empty:0|products-empty:0",
      localizedDiscoveryFootprint: "pages-empty:0|products-empty:0",
      localizedDiscoverySummaryFootprint:
        "cultures:0|pages:0|products:0|pages-empty:0|products-empty:0",
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

  assert.deepEqual(
    summarizeSeoMetadataHealth({
      canonicalPath: "",
      noIndex: false,
    }),
    {
      canonicalPath: "",
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
      seoTargetFootprint: "indexable|",
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

  assert.deepEqual(
    summarizeMemberPagedCollectionHealth({
      status: "unknown",
      data: null,
    }),
    {
      status: "unknown",
      itemCount: 0,
      totalCount: 0,
      page: 1,
      pageSize: 0,
      memberCollectionWorkflowFootprint: "status:unknown|page:1|size:0|items:0|total:0",
    },
  );

  assert.deepEqual(
    summarizeMemberDetailHealth({
      status: "unknown",
      data: null,
    }),
    {
      status: "unknown",
      hasData: false,
      memberWorkflowFootprint: "detail:unknown|has-data:no",
    },
  );
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





















































