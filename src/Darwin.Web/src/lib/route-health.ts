import type { PublicStorefrontContext } from "@/features/storefront/public-storefront-context";
import { canonicalizeLanguageAlternates } from "@/lib/localized-alternates";

type CatalogBrowseLike = {
  categoriesResult: { status: string; data?: { items?: unknown[] } | null };
  productsResult: { status: string; data?: { items?: unknown[] } | null };
};

type CatalogDetailLike = {
  categoriesResult: { status: string; data?: { items?: unknown[] } | null };
  productResult: { status: string; data?: unknown | null };
};

type ProductDetailRelatedLike = {
  status: string;
  data?: { items?: unknown[] } | null;
};

type CmsBrowseLike = {
  pagesResult: { status: string; data?: { items?: unknown[] } | null };
};

type CmsDetailLike = {
  pageResult: { status: string; data?: unknown | null };
  relatedPagesResult?: { status: string; data?: { items?: unknown[] } | null };
};

type IdentityContextLike = {
  profileResult: { status: string };
  preferencesResult: { status: string };
  customerContextResult: { status: string };
  addressesResult: { status: string; data?: unknown[] | null };
};

type CommerceSummaryLike = {
  ordersResult: { status: string; data?: { items?: unknown[] } | null };
  invoicesResult: { status: string; data?: { items?: unknown[] } | null };
  loyaltyOverviewResult: { status: string };
};

type MemberPagedCollectionLike = {
  status: string;
  data?: {
    items?: unknown[];
    total?: number;
    request?: {
      page?: number;
      pageSize?: number;
    };
  } | null;
};

type MemberDetailLike = {
  status: string;
  data?: unknown | null;
};

type CartViewModelLike = {
  anonymousId: string | null;
  status: string;
  cart: {
    items: unknown[];
    currency: string;
    grandTotalGrossMinor: number;
    couponCode?: string | null;
  } | null;
};

type ConfirmationResultLike = {
  status: string;
  data?: {
    lines?: unknown[];
    payments?: Array<{ status: string }>;
  } | null;
};

type HomeCategorySpotlightResultLike = Array<{
  categorySlug: string;
  categoryProductsResult: { status: string; data?: { items?: unknown[] } | null };
}>;

type ShoppingContextLike = {
  anonymousCartId?: string | null;
  cartResult: { status: string; data?: { items?: unknown[] } | null };
  cartSnapshots: unknown[];
  cartLinkedProductSlugs: string[];
};

type ShellMenuLike = {
  status: string;
  data?: {
    items?: unknown[];
  } | null;
};

type LocalizedInventoryLike = Array<{
  culture: string;
  items: unknown[];
}>;

type ResultWithStorefront = {
  storefrontContext: PublicStorefrontContext;
};

function getSeoMetadataState(noIndex: boolean, alternateCount: number) {
  return noIndex
    ? "private"
    : alternateCount > 0
      ? "localized"
      : "single-locale";
}

function getSeoIndexability(noIndex: boolean) {
  return noIndex ? "noindex" : "indexable";
}

function getLanguageAlternateState(alternateCount: number) {
  return alternateCount > 0 ? "present" : "missing";
}

function countItems(data?: { items?: unknown[] } | null) {
  return data?.items?.length ?? 0;
}

export function summarizePublicStorefrontHealth(
  storefrontContext: PublicStorefrontContext,
) {
  return {
    cmsStatus: storefrontContext.cmsPagesStatus,
    cmsCount: storefrontContext.cmsPages.length,
    categoriesStatus: storefrontContext.categoriesStatus,
    categoryCount: storefrontContext.categories.length,
    productsStatus: storefrontContext.productsStatus,
    productCount: storefrontContext.products.length,
    cartStatus: storefrontContext.storefrontCartStatus,
    cartLinkedCount: storefrontContext.cartLinkedProductSlugs.length,
  };
}

export function summarizeStorefrontContinuationHealth(result: {
  cmsPagesStatus: string;
  cmsPages: unknown[];
  categoriesStatus: string;
  categories: unknown[];
  productsStatus: string;
  products: unknown[];
}) {
  return {
    cmsStatus: result.cmsPagesStatus,
    cmsCount: result.cmsPages.length,
    categoriesStatus: result.categoriesStatus,
    categoryCount: result.categories.length,
    productsStatus: result.productsStatus,
    productCount: result.products.length,
  };
}

export function summarizeCatalogRouteHealth(
  result: ResultWithStorefront & {
    browseContext?: CatalogBrowseLike;
    detailContext?: CatalogDetailLike;
  },
) {
  return {
    ...summarizePublicStorefrontHealth(result.storefrontContext),
    coreCategoriesStatus:
      result.browseContext?.categoriesResult.status ??
      result.detailContext?.categoriesResult.status ??
      "unknown",
    coreCategoryCount:
      countItems(result.browseContext?.categoriesResult.data) ||
      countItems(result.detailContext?.categoriesResult.data),
    coreProductsStatus:
      result.browseContext?.productsResult.status ??
      result.detailContext?.productResult.status ??
      "unknown",
    coreProductCount:
      countItems(result.browseContext?.productsResult.data) ||
      (result.detailContext?.productResult.data ? 1 : 0),
  };
}

export function summarizeCatalogIndexPageHealth(
  result: ResultWithStorefront & {
    browseContext: CatalogBrowseLike;
    visibleWindow: { items: unknown[]; total: number };
    matchingSetResult?: { status: string } | null;
    matchingProductsTotal: number;
    facetSummary: {
      offerCount: number;
      baseCount: number;
      withImageCount: number;
      missingImageCount: number;
      heroOfferCount: number;
      valueOfferCount: number;
    };
    hasBrowseLens: boolean;
  },
) {
  return {
    ...summarizeCatalogRouteHealth(result),
    browseMode: result.hasBrowseLens ? "windowed" : "paged",
    visibleCount: result.visibleWindow.items.length,
    visibleTotal: result.visibleWindow.total,
    matchingStatus: result.matchingSetResult?.status ?? "not-requested",
    matchingProductsTotal: result.matchingProductsTotal,
    offerCount: result.facetSummary.offerCount,
    baseCount: result.facetSummary.baseCount,
    withImageCount: result.facetSummary.withImageCount,
    missingImageCount: result.facetSummary.missingImageCount,
    heroOfferCount: result.facetSummary.heroOfferCount,
    valueOfferCount: result.facetSummary.valueOfferCount,
  };
}

export function summarizeCatalogBrowseCoreHealth(result: CatalogBrowseLike) {
  return {
    categoriesStatus: result.categoriesResult.status,
    categoryCount: countItems(result.categoriesResult.data),
    productsStatus: result.productsResult.status,
    productCount: countItems(result.productsResult.data),
  };
}

export function summarizeCatalogDetailCoreHealth(result: CatalogDetailLike) {
  return {
    categoriesStatus: result.categoriesResult.status,
    categoryCount: countItems(result.categoriesResult.data),
    productStatus: result.productResult.status,
    hasProduct: Boolean(result.productResult.data),
  };
}

export function summarizeProductDetailRelatedHealth(
  result: ProductDetailRelatedLike,
) {
  return {
    status: result.status,
    relatedCount: countItems(result.data),
  };
}

export function summarizeCmsRouteHealth(
  result: ResultWithStorefront & {
    browseContext?: CmsBrowseLike;
    detailContext?: CmsDetailLike;
  },
) {
  return {
    ...summarizePublicStorefrontHealth(result.storefrontContext),
    corePagesStatus:
      result.browseContext?.pagesResult.status ??
      result.detailContext?.pageResult?.status ??
      "unknown",
    corePageCount:
      countItems(result.browseContext?.pagesResult.data) ||
      countItems(result.detailContext?.relatedPagesResult?.data) ||
      (result.detailContext?.pageResult.data ? 1 : 0),
  };
}

export function summarizeCmsIndexPageHealth(
  result: ResultWithStorefront & {
    browseContext: CmsBrowseLike;
    visibleWindow: { items: unknown[]; total: number };
    matchingSetResult?: { status: string } | null;
    matchingItemsTotal: number;
    metadataSummary: {
      readyCount: number;
      attentionCount: number;
      missingMetaTitleCount: number;
      missingMetaDescriptionCount: number;
      missingBothCount: number;
    };
    hasBrowseLens: boolean;
  },
) {
  return {
    ...summarizeCmsRouteHealth(result),
    browseMode: result.hasBrowseLens ? "windowed" : "paged",
    visibleCount: result.visibleWindow.items.length,
    visibleTotal: result.visibleWindow.total,
    matchingStatus: result.matchingSetResult?.status ?? "not-requested",
    matchingItemsTotal: result.matchingItemsTotal,
    readyCount: result.metadataSummary.readyCount,
    attentionCount: result.metadataSummary.attentionCount,
    missingMetaTitleCount: result.metadataSummary.missingMetaTitleCount,
    missingMetaDescriptionCount:
      result.metadataSummary.missingMetaDescriptionCount,
    missingBothCount: result.metadataSummary.missingBothCount,
  };
}

export function summarizeCmsBrowseCoreHealth(result: CmsBrowseLike) {
  return {
    pagesStatus: result.pagesResult.status,
    pageCount: countItems(result.pagesResult.data),
  };
}

export function summarizeCmsDetailCoreHealth(result: CmsDetailLike) {
  return {
    pageStatus: result.pageResult.status,
    hasPage: Boolean(result.pageResult.data),
    relatedSeedStatus: result.relatedPagesResult?.status ?? "unknown",
    relatedSeedCount: countItems(result.relatedPagesResult?.data),
  };
}

export function summarizeMemberDashboardHealth(
  result: ResultWithStorefront & {
    identityContext: IdentityContextLike;
    commerceSummaryContext: CommerceSummaryLike;
    loyaltyBusinessesResult: { status: string; data?: { items?: unknown[] } | null };
  },
) {
  return {
    ...summarizePublicStorefrontHealth(result.storefrontContext),
    profileStatus: result.identityContext.profileResult.status,
    preferencesStatus: result.identityContext.preferencesResult.status,
    customerContextStatus: result.identityContext.customerContextResult.status,
    addressesStatus: result.identityContext.addressesResult.status,
    addressCount: result.identityContext.addressesResult.data?.length ?? 0,
    ordersStatus: result.commerceSummaryContext.ordersResult.status,
    orderCount: countItems(result.commerceSummaryContext.ordersResult.data),
    invoicesStatus: result.commerceSummaryContext.invoicesResult.status,
    invoiceCount: countItems(result.commerceSummaryContext.invoicesResult.data),
    loyaltyStatus: result.commerceSummaryContext.loyaltyOverviewResult.status,
    loyaltyBusinessesStatus: result.loyaltyBusinessesResult.status,
    loyaltyBusinessCount: countItems(result.loyaltyBusinessesResult.data),
  };
}

export function summarizeMemberEditorHealth(
  result: ResultWithStorefront & { identityContext: IdentityContextLike },
) {
  return {
    ...summarizePublicStorefrontHealth(result.storefrontContext),
    profileStatus: result.identityContext.profileResult.status,
    preferencesStatus: result.identityContext.preferencesResult.status,
    customerContextStatus: result.identityContext.customerContextResult.status,
    addressesStatus: result.identityContext.addressesResult.status,
    addressCount: result.identityContext.addressesResult.data?.length ?? 0,
  };
}

export function summarizeMemberCollectionHealth(
  result: ResultWithStorefront & {
    ordersResult?: { status: string; data?: { items?: unknown[] } | null };
    invoicesResult?: { status: string; data?: { items?: unknown[] } | null };
  },
) {
  return {
    ...summarizePublicStorefrontHealth(result.storefrontContext),
    ordersStatus: result.ordersResult?.status ?? "unknown",
    orderCount: countItems(result.ordersResult?.data),
    invoicesStatus: result.invoicesResult?.status ?? "unknown",
    invoiceCount: countItems(result.invoicesResult?.data),
  };
}

export function summarizeMemberPagedCollectionHealth(
  result: MemberPagedCollectionLike,
) {
  return {
    status: result.status,
    itemCount: countItems(result.data),
    totalCount: result.data?.total ?? 0,
    page: result.data?.request?.page ?? 1,
    pageSize: result.data?.request?.pageSize ?? countItems(result.data),
  };
}

export function summarizeMemberDetailHealth(result: MemberDetailLike) {
  return {
    status: result.status,
    hasData: Boolean(result.data),
  };
}

export function summarizeHomeDiscoveryHealth(result: {
  storefrontContext: PublicStorefrontContext;
  pagesResult: { status: string; data?: { items?: unknown[] } | null };
  categoriesResult: { status: string; data?: { items?: unknown[] } | null };
  productsResult: { status: string; data?: { items?: unknown[] } | null };
  categorySpotlights: Array<{ status: string }>;
}) {
  return {
    ...summarizePublicStorefrontHealth(result.storefrontContext),
    homePagesStatus: result.pagesResult.status,
    homePageCount: countItems(result.pagesResult.data),
    homeCategoriesStatus: result.categoriesResult.status,
    homeCategoryCount: countItems(result.categoriesResult.data),
    homeProductsStatus: result.productsResult.status,
    homeProductCount: countItems(result.productsResult.data),
    spotlightCount: result.categorySpotlights.length,
    degradedSpotlightCount: result.categorySpotlights.filter(
      (spotlight) => spotlight.status !== "ok",
    ).length,
  };
}

export function summarizeHomeRouteHealth(result: {
  memberSession: unknown | null;
  parts: unknown[];
}) {
  return {
    memberSessionState: result.memberSession ? "present" : "missing",
    partCount: result.parts.length,
  };
}

export function summarizeHomeCategorySpotlightsHealth(
  result: HomeCategorySpotlightResultLike,
) {
  return {
    spotlightCount: result.length,
    degradedSpotlightCount: result.filter(
      (entry) => entry.categoryProductsResult.status !== "ok",
    ).length,
    spotlightProductCount: result.reduce(
      (total, entry) => total + countItems(entry.categoryProductsResult.data),
      0,
    ),
  };
}

export function summarizeCommerceRouteHealth(
  result: ResultWithStorefront & {
    model?: CartViewModelLike;
    memberSession?: unknown | null;
    identityContext?: IdentityContextLike | null;
    commerceSummaryContext?: CommerceSummaryLike | null;
    confirmationResult?: ConfirmationResultLike;
  },
) {
  return {
    ...summarizePublicStorefrontHealth(result.storefrontContext),
    cartModelStatus: result.model?.status ?? "unknown",
    cartItemCount: result.model?.cart?.items.length ?? 0,
    hasAnonymousCart: Boolean(result.model?.anonymousId),
    hasCoupon: Boolean(result.model?.cart?.couponCode),
    memberSessionState: result.memberSession ? "present" : "missing",
    profileStatus: result.identityContext?.profileResult.status ?? "unknown",
    preferencesStatus:
      result.identityContext?.preferencesResult.status ?? "unknown",
    addressesStatus: result.identityContext?.addressesResult.status ?? "unknown",
    invoiceSummaryStatus:
      result.commerceSummaryContext?.invoicesResult.status ?? "unknown",
    invoiceSummaryCount: countItems(
      result.commerceSummaryContext?.invoicesResult.data,
    ),
    orderSummaryStatus:
      result.commerceSummaryContext?.ordersResult.status ?? "unknown",
    orderSummaryCount: countItems(
      result.commerceSummaryContext?.ordersResult.data,
    ),
    confirmationStatus: result.confirmationResult?.status ?? "unknown",
    confirmationLineCount: result.confirmationResult?.data?.lines?.length ?? 0,
    confirmationPaymentCount:
      result.confirmationResult?.data?.payments?.length ?? 0,
    recordedPaidPaymentCount:
      result.confirmationResult?.data?.payments?.filter(
        (payment) => payment.status === "Paid",
      ).length ?? 0,
  };
}

export function summarizeConfirmationResultHealth(
  result: ConfirmationResultLike,
) {
  return {
    status: result.status,
    lineCount: result.data?.lines?.length ?? 0,
    paymentCount: result.data?.payments?.length ?? 0,
    paidPaymentCount:
      result.data?.payments?.filter((payment) => payment.status === "Paid")
        .length ?? 0,
  };
}

export function summarizeCartPageHealth(result: {
  routeContext: {
    model: CartViewModelLike;
    memberSession?: unknown | null;
    identityContext?: IdentityContextLike | null;
  };
  followUpProducts: unknown[];
}) {
  return {
    cartStatus: result.routeContext.model.status,
    cartItemCount: result.routeContext.model.cart?.items.length ?? 0,
    memberSessionState: result.routeContext.memberSession ? "present" : "missing",
    addressesStatus:
      result.routeContext.identityContext?.addressesResult.status ?? "unauthenticated",
    followUpProductCount: result.followUpProducts.length,
  };
}

export function summarizeCheckoutPageHealth(result: {
  routeContext: {
    model: CartViewModelLike;
    memberSession?: unknown | null;
    identityContext?: IdentityContextLike | null;
    commerceSummaryContext?: CommerceSummaryLike | null;
  };
}) {
  return {
    cartStatus: result.routeContext.model.status,
    cartItemCount: result.routeContext.model.cart?.items.length ?? 0,
    memberSessionState: result.routeContext.memberSession ? "present" : "missing",
    addressesStatus:
      result.routeContext.identityContext?.addressesResult.status ?? "unauthenticated",
    invoicesStatus:
      result.routeContext.commerceSummaryContext?.invoicesResult.status ??
      "unauthenticated",
    invoiceCount: countItems(
      result.routeContext.commerceSummaryContext?.invoicesResult.data,
    ),
  };
}

export function summarizeConfirmationPageHealth(result: {
  routeContext: {
    confirmationResult: ConfirmationResultLike;
    memberSession?: unknown | null;
    commerceSummaryContext?: CommerceSummaryLike | null;
  };
  followUpProducts: unknown[];
}) {
  return {
    confirmationStatus: result.routeContext.confirmationResult.status,
    lineCount: result.routeContext.confirmationResult.data?.lines?.length ?? 0,
    memberSessionState: result.routeContext.memberSession ? "present" : "missing",
    ordersStatus:
      result.routeContext.commerceSummaryContext?.ordersResult.status ??
      "unauthenticated",
    invoicesStatus:
      result.routeContext.commerceSummaryContext?.invoicesResult.status ??
      "unauthenticated",
    followUpProductCount: result.followUpProducts.length,
  };
}

export function summarizeStorefrontShoppingHealth(result: ShoppingContextLike) {
  return {
    anonymousCartState: result.anonymousCartId ? "present" : "missing",
    liveCartStatus: result.cartResult.status,
    liveCartItemCount: result.cartResult.data?.items?.length ?? 0,
    snapshotCount: result.cartSnapshots.length,
    cartLinkedCount: result.cartLinkedProductSlugs.length,
  };
}

export function summarizeShellHealth(result: ShellMenuLike) {
  return {
    menuStatus: result.status,
    menuItemCount: result.data?.items?.length ?? 0,
  };
}

export function summarizeShellModelHealth(result: {
  culture: string;
  menuSource: string;
  menuStatus: string;
  primaryNavigation: unknown[];
  utilityLinks: unknown[];
  footerGroups: unknown[];
}) {
  return {
    culture: result.culture,
    menuSource: result.menuSource,
    menuStatus: result.menuStatus,
    primaryNavigationCount: result.primaryNavigation.length,
    utilityLinkCount: result.utilityLinks.length,
    footerGroupCount: result.footerGroups.length,
  };
}

export function summarizeCartViewModelHealth(result: CartViewModelLike) {
  return {
    anonymousCartState: result.anonymousId ? "present" : "missing",
    cartStatus: result.status,
    cartItemCount: result.cart?.items.length ?? 0,
    hasCoupon: Boolean(result.cart?.couponCode),
  };
}

export function summarizeMemberIdentityHealth(result: IdentityContextLike) {
  return {
    profileStatus: result.profileResult.status,
    preferencesStatus: result.preferencesResult.status,
    customerContextStatus: result.customerContextResult.status,
    addressesStatus: result.addressesResult.status,
    addressCount: result.addressesResult.data?.length ?? 0,
  };
}

export function summarizePublicAuthRouteHealth(result: {
  storefrontContext: PublicStorefrontContext;
}) {
  return summarizePublicStorefrontHealth(result.storefrontContext);
}

export function summarizeAccountPageHealth(result: {
  session: unknown | null;
  publicRouteContext?: {
    storefrontContext: PublicStorefrontContext;
  } | null;
  memberRouteContext?: {
    storefrontContext: PublicStorefrontContext;
    identityContext: IdentityContextLike;
    commerceSummaryContext: CommerceSummaryLike;
    loyaltyBusinessesResult: { status: string; data?: { items?: unknown[] } | null };
  } | null;
}) {
  if (result.memberRouteContext) {
    return {
      sessionState: "present",
      ...summarizeMemberDashboardHealth({
        storefrontContext: result.memberRouteContext.storefrontContext,
        identityContext: result.memberRouteContext.identityContext,
        commerceSummaryContext: result.memberRouteContext.commerceSummaryContext,
        loyaltyBusinessesResult: result.memberRouteContext.loyaltyBusinessesResult,
      }),
    };
  }

  return {
    sessionState: "missing",
    ...(result.publicRouteContext
      ? summarizePublicStorefrontHealth(result.publicRouteContext.storefrontContext)
      : {}),
  };
}

export function summarizeProtectedMemberEntryHealth(result: {
  session: unknown | null;
  storefrontContext: PublicStorefrontContext | null;
}) {
  return {
    sessionState: result.session ? "present" : "missing",
    ...(result.storefrontContext
      ? summarizePublicStorefrontHealth(result.storefrontContext)
      : {}),
  };
}

export function summarizeMemberCommerceSummaryHealth(
  result: CommerceSummaryLike,
) {
  return {
    ordersStatus: result.ordersResult.status,
    orderCount: countItems(result.ordersResult.data),
    invoicesStatus: result.invoicesResult.status,
    invoiceCount: countItems(result.invoicesResult.data),
    loyaltyStatus: result.loyaltyOverviewResult.status,
  };
}

export function summarizeLocalizedInventoryHealth(
  result: LocalizedInventoryLike,
) {
  const emptyCultureCount = result.filter((entry) => entry.items.length === 0).length;
  const localizedItemCount = result.reduce(
    (total, entry) => total + entry.items.length,
    0,
  );

  return {
    localizedDiscoveryState:
      result.length > 0 || localizedItemCount > 0 ? "present" : "empty",
    localizedCultureCount: result.length,
    localizedItemCount,
    emptyCultureCount,
    localizedDiscoveryDetailFootprint: `cultures:${result.length}|items:${localizedItemCount}|empty:${emptyCultureCount}`,
    localizedInventoryFootprint: `cultures:${result.length}|items:${localizedItemCount}|empty:${emptyCultureCount}`,
    localizedInventorySummaryFootprint: `cultures:${result.length}|items:${localizedItemCount}|empty:${emptyCultureCount}`,
  };
}

export function summarizeLocalizedDiscoveryInventoryHealth(result: {
  pages: LocalizedInventoryLike;
  products: LocalizedInventoryLike;
}) {
  const localizedCultureCount = Math.max(result.pages.length, result.products.length);
  const localizedPageCount = result.pages.reduce(
    (total, entry) => total + entry.items.length,
    0,
  );
  const localizedProductCount = result.products.reduce(
    (total, entry) => total + entry.items.length,
    0,
  );
  const emptyPageCultureCount = result.pages.filter(
    (entry) => entry.items.length === 0,
  ).length;
  const emptyProductCultureCount = result.products.filter(
    (entry) => entry.items.length === 0,
  ).length;

  return {
    localizedDiscoveryState:
      localizedCultureCount > 0 ||
      localizedPageCount > 0 ||
      localizedProductCount > 0
        ? "present"
        : "empty",
    localizedCultureCount,
    localizedPageCount,
    localizedProductCount,
    emptyPageCultureCount,
    emptyProductCultureCount,
    localizedDiscoveryDetailFootprint: `pages-empty:${emptyPageCultureCount}|products-empty:${emptyProductCultureCount}`,
    localizedDiscoveryFootprint: `pages-empty:${emptyPageCultureCount}|products-empty:${emptyProductCultureCount}`,
    localizedDiscoverySummaryFootprint: `cultures:${localizedCultureCount}|pages:${localizedPageCount}|products:${localizedProductCount}|pages-empty:${emptyPageCultureCount}|products-empty:${emptyProductCultureCount}`,
  };
}

export function summarizeLocalizedAlternatesMapHealth(
  result: Map<string, Record<string, string>>,
) {
  const alternates = Array.from(result.values());
  const alternateCount = alternates.reduce(
    (total, entry) => total + Object.keys(entry).length,
    0,
  );
  const multiCultureItemCount = alternates.filter(
    (entry) => Object.keys(entry).length > 1,
  ).length;

  return {
    localizedDiscoveryState:
      result.size > 0 || alternateCount > 0 ? "present" : "empty",
    itemCount: result.size,
    alternateCount,
    multiCultureItemCount,
    localizedDiscoveryDetailFootprint: `items:${result.size}|alternates:${alternateCount}|multi:${multiCultureItemCount}`,
    alternateMapFootprint: `items:${result.size}|alternates:${alternateCount}|multi:${multiCultureItemCount}`,
    alternateMapSummaryFootprint: `items:${result.size}|alternates:${alternateCount}|multi:${multiCultureItemCount}`,
  };
}

export function summarizePublicSitemapHealth(result: {
  entries: unknown[];
  staticEntryCount: number;
  cmsEntryCount: number;
  productEntryCount: number;
}) {
  return {
    localizedDiscoveryState: result.entries.length > 0 ? "present" : "empty",
    totalEntryCount: result.entries.length,
    staticEntryCount: result.staticEntryCount,
    cmsEntryCount: result.cmsEntryCount,
    productEntryCount: result.productEntryCount,
    localizedDiscoveryDetailFootprint: `static:${result.staticEntryCount}|cms:${result.cmsEntryCount}|products:${result.productEntryCount}`,
    sitemapCompositionFootprint: `static:${result.staticEntryCount}|cms:${result.cmsEntryCount}|products:${result.productEntryCount}`,
    sitemapSummaryFootprint: `total:${result.entries.length}|static:${result.staticEntryCount}|cms:${result.cmsEntryCount}|products:${result.productEntryCount}`,
  };
}

export function summarizeSeoMetadataHealth(result: {
  canonicalPath: string;
  noIndex: boolean;
  languageAlternates?: Record<string, string>;
}) {
  const normalizedAlternates = canonicalizeLanguageAlternates(
    result.languageAlternates,
  );
  const alternateCultures = Object.keys(normalizedAlternates ?? {});
  const seoIndexability = getSeoIndexability(result.noIndex);
  const seoMetadataState = getSeoMetadataState(
    result.noIndex,
    alternateCultures.length,
  );

  return {
    canonicalPath: result.canonicalPath,
    noIndex: result.noIndex,
    seoIndexability,
    seoMetadataState,
    seoVisibilityFootprint: `${seoIndexability}|${seoMetadataState}`,
    languageAlternateState: getLanguageAlternateState(alternateCultures.length),
    languageAlternateCount: alternateCultures.length,
    languageAlternateFootprint:
      alternateCultures.length > 0 ? alternateCultures.join("|") : "none",
    seoAlternateDetailFootprint:
      alternateCultures.length > 0 ? alternateCultures.join("|") : "none",
    seoAlternateSummaryFootprint:
      alternateCultures.length > 0
        ? `alternates:${alternateCultures.length}[${alternateCultures.join("|")}]`
        : "alternates:none",
    seoSummaryFootprint: `${seoIndexability}|alternates:${alternateCultures.length}[${alternateCultures.length > 0 ? alternateCultures.join("|") : "none"}]`,
    seoTargetFootprint: `${seoIndexability}|${result.canonicalPath}`,
  };
}
