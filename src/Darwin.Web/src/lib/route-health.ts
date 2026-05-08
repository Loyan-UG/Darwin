import { summarizeCatalogPromotionLanes } from "@/features/catalog/promotion-lanes";
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

type ProductPromotionSummaryLike = {
  id: string;
  slug: string;
  name: string;
  priceMinor: number;
  currency: string;
  shortDescription?: string | null;
  compareAtPriceMinor?: number | null;
  primaryImageUrl?: string | null;
};

type ResultWithStorefront = {
  storefrontContext: PublicStorefrontContext;
};

export function getSeoMetadataState(noIndex: boolean, alternateCount: number) {
  return noIndex
    ? "private"
    : alternateCount > 0
      ? "localized"
      : "single-locale";
}

export function getSeoIndexability(noIndex: boolean) {
  return noIndex ? "noindex" : "indexable";
}

export function getLanguageAlternateState(alternateCount: number) {
  return alternateCount > 0 ? "present" : "missing";
}

function countItems(data?: { items?: unknown[] } | null) {
  return data?.items?.length ?? 0;
}

export function summarizePromotionLaneHealth(products: ProductPromotionSummaryLike[]) {
  const lanes = summarizeCatalogPromotionLanes(products);
  const heroOfferCount = lanes.find((entry) => entry.lane === "hero-offers")?.count ?? 0;
  const valueOfferCount = lanes.find((entry) => entry.lane === "value-offers")?.count ?? 0;
  const liveOfferCount = lanes.find((entry) => entry.lane === "live-offers")?.count ?? 0;
  const baseAssortmentCount =
    lanes.find((entry) => entry.lane === "base-assortment")?.count ?? 0;

  return {
    heroOfferCount,
    valueOfferCount,
    liveOfferCount,
    baseAssortmentCount,
    promotionLaneFootprint:
      `hero:${heroOfferCount}|value:${valueOfferCount}|live:${liveOfferCount}|base:${baseAssortmentCount}`,
  };
}

type StorefrontHealthSummaryInput = {
  cmsStatus: string;
  cmsCount: number;
  categoriesStatus: string;
  categoryCount: number;
  productsStatus: string;
  productCount: number;
  products: ProductPromotionSummaryLike[];
};

type StorefrontHealthSummaryWithCart = StorefrontHealthSummaryInput & {
  cartStatus?: string;
  cartLinkedCount?: number;
};

export function buildStorefrontHealthSummary(
  input: StorefrontHealthSummaryInput & {
    cartStatus: string;
    cartLinkedCount?: number;
  },
): {
  cmsStatus: string;
  cmsCount: number;
  categoriesStatus: string;
  categoryCount: number;
  productsStatus: string;
  productCount: number;
  heroOfferCount: number;
  valueOfferCount: number;
  liveOfferCount: number;
  baseAssortmentCount: number;
  promotionLaneFootprint: string;
  cartStatus: string;
  cartLinkedCount: number;
};
export function buildStorefrontHealthSummary(
  input: StorefrontHealthSummaryInput,
): {
  cmsStatus: string;
  cmsCount: number;
  categoriesStatus: string;
  categoryCount: number;
  productsStatus: string;
  productCount: number;
  heroOfferCount: number;
  valueOfferCount: number;
  liveOfferCount: number;
  baseAssortmentCount: number;
  promotionLaneFootprint: string;
};
export function buildStorefrontHealthSummary(input: StorefrontHealthSummaryWithCart) {
  return {
    cmsStatus: input.cmsStatus,
    cmsCount: input.cmsCount,
    categoriesStatus: input.categoriesStatus,
    categoryCount: input.categoryCount,
    productsStatus: input.productsStatus,
    productCount: input.productCount,
    ...summarizePromotionLaneHealth(input.products),
    ...(input.cartStatus !== undefined
      ? {
          cartStatus: input.cartStatus,
          cartLinkedCount: input.cartLinkedCount ?? 0,
        }
      : {}),
  };
}

export function summarizePublicStorefrontHealth(
  storefrontContext: PublicStorefrontContext,
) {
  return buildStorefrontHealthSummary({
    cmsStatus: storefrontContext.cmsPagesStatus,
    cmsCount: storefrontContext.cmsPages.length,
    categoriesStatus: storefrontContext.categoriesStatus,
    categoryCount: storefrontContext.categories.length,
    productsStatus: storefrontContext.productsStatus,
    productCount: storefrontContext.products.length,
    products: storefrontContext.products,
    cartStatus: storefrontContext.storefrontCartStatus,
    cartLinkedCount: storefrontContext.cartLinkedProductSlugs.length,
  });
}


export function summarizeStorefrontContextFootprint(result: {
  cmsStatus: string;
  cmsCount: number;
  categoriesStatus: string;
  categoryCount: number;
  productsStatus: string;
  productCount: number;
  cartStatus: string;
}) {
  return `cms:${result.cmsStatus}:${result.cmsCount}|categories:${result.categoriesStatus}:${result.categoryCount}|products:${result.productsStatus}:${result.productCount}|cart:${result.cartStatus}`;
}
export function summarizeStorefrontContinuationHealth(result: {
  cmsPagesStatus: string;
  cmsPages: unknown[];
  categoriesStatus: string;
  categories: unknown[];
  productsStatus: string;
  products: Array<{
    id: string;
    slug: string;
    name: string;
    priceMinor: number;
    currency: string;
    shortDescription?: string | null;
    compareAtPriceMinor?: number | null;
    primaryImageUrl?: string | null;
  }>;
}) {
  return buildStorefrontHealthSummary({
    cmsStatus: result.cmsPagesStatus,
    cmsCount: result.cmsPages.length,
    categoriesStatus: result.categoriesStatus,
    categoryCount: result.categories.length,
    productsStatus: result.productsStatus,
    productCount: result.products.length,
    products: result.products,
  });
}

export function summarizeCatalogRouteHealth(
  result: ResultWithStorefront & {
    browseContext?: CatalogBrowseLike;
    detailContext?: CatalogDetailLike;
  },
) {
  const storefrontSummary = summarizePublicStorefrontHealth(result.storefrontContext);

  return {
    ...storefrontSummary,
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
  const browseMode = result.hasBrowseLens ? "windowed" : "paged";
  const matchingStatus = result.matchingSetResult?.status ?? "not-requested";
  const promotionLaneFootprint = `hero:${result.facetSummary.heroOfferCount}|value:${result.facetSummary.valueOfferCount}|live:${result.facetSummary.offerCount}|base:${result.facetSummary.baseCount}`;
  const storefrontSummary = summarizePublicStorefrontHealth(result.storefrontContext);

  return {
    ...summarizeCatalogRouteHealth(result),
    browseMode,
    visibleCount: result.visibleWindow.items.length,
    visibleTotal: result.visibleWindow.total,
    matchingStatus,
    matchingProductsTotal: result.matchingProductsTotal,
    offerCount: result.facetSummary.offerCount,
    baseCount: result.facetSummary.baseCount,
    withImageCount: result.facetSummary.withImageCount,
    missingImageCount: result.facetSummary.missingImageCount,
    heroOfferCount: result.facetSummary.heroOfferCount,
    valueOfferCount: result.facetSummary.valueOfferCount,
    liveOfferCount: result.facetSummary.offerCount,
    baseAssortmentCount: result.facetSummary.baseCount,
    promotionLaneFootprint,
    catalogBrowseWorkflowFootprint: `mode:${browseMode}|matching:${matchingStatus}:${result.matchingProductsTotal}|visible:${result.visibleWindow.items.length}/${result.visibleWindow.total}|lanes:${promotionLaneFootprint}`,
    catalogContextFootprint: `cms:${storefrontSummary.cmsStatus}:${storefrontSummary.cmsCount}|products:${storefrontSummary.productsStatus}:${storefrontSummary.productCount}|cart:${storefrontSummary.cartStatus}`,
  };
}

export function summarizeCatalogBrowseCoreHealth(result: CatalogBrowseLike) {
  const categoryCount = countItems(result.categoriesResult.data);
  const productCount = countItems(result.productsResult.data);

  return {
    categoriesStatus: result.categoriesResult.status,
    categoryCount,
    productsStatus: result.productsResult.status,
    productCount,
    catalogBrowseCoreFootprint: `categories:${result.categoriesResult.status}:${categoryCount}|products:${result.productsResult.status}:${productCount}`,
  };
}

export function summarizeCatalogDetailCoreHealth(result: CatalogDetailLike) {
  const categoryCount = countItems(result.categoriesResult.data);
  const hasProduct = Boolean(result.productResult.data);

  return {
    categoriesStatus: result.categoriesResult.status,
    categoryCount,
    productStatus: result.productResult.status,
    hasProduct,
    catalogDetailWorkflowFootprint: `product:${result.productResult.status}:${hasProduct ? "present" : "missing"}|categories:${result.categoriesResult.status}:${categoryCount}`,
  };
}

export function summarizeProductDetailRelatedHealth(
  result: ProductDetailRelatedLike,
) {
  const relatedCount = countItems(result.data);

  return {
    status: result.status,
    relatedCount,
    productRelatedWorkflowFootprint: `status:${result.status}|related:${relatedCount}`,
  };
}

export function summarizeCmsRouteHealth(
  result: ResultWithStorefront & {
    browseContext?: CmsBrowseLike;
    detailContext?: CmsDetailLike;
  },
) {
  const storefrontSummary = summarizePublicStorefrontHealth(result.storefrontContext);
  const corePagesStatus =
    result.browseContext?.pagesResult.status ??
    result.detailContext?.pageResult?.status ??
    "unknown";
  const corePageCount =
    countItems(result.browseContext?.pagesResult.data) ||
    countItems(result.detailContext?.relatedPagesResult?.data) ||
    (result.detailContext?.pageResult.data ? 1 : 0);

  return {
    ...storefrontSummary,
    corePagesStatus,
    corePageCount,
    cmsWorkflowFootprint: `core:${corePagesStatus}:${corePageCount}|lanes:${storefrontSummary.promotionLaneFootprint}`,
    cmsContextFootprint: `categories:${storefrontSummary.categoriesStatus}:${storefrontSummary.categoryCount}|products:${storefrontSummary.productsStatus}:${storefrontSummary.productCount}|cart:${storefrontSummary.cartStatus}`,
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
  const browseMode = result.hasBrowseLens ? "windowed" : "paged";
  const matchingStatus = result.matchingSetResult?.status ?? "not-requested";

  return {
    ...summarizeCmsRouteHealth(result),
    browseMode,
    visibleCount: result.visibleWindow.items.length,
    visibleTotal: result.visibleWindow.total,
    matchingStatus,
    matchingItemsTotal: result.matchingItemsTotal,
    readyCount: result.metadataSummary.readyCount,
    attentionCount: result.metadataSummary.attentionCount,
    missingMetaTitleCount: result.metadataSummary.missingMetaTitleCount,
    missingMetaDescriptionCount:
      result.metadataSummary.missingMetaDescriptionCount,
    missingBothCount: result.metadataSummary.missingBothCount,
    cmsBrowseWorkflowFootprint: `mode:${browseMode}|matching:${matchingStatus}:${result.matchingItemsTotal}|visible:${result.visibleWindow.items.length}/${result.visibleWindow.total}|attention:${result.metadataSummary.attentionCount}|ready:${result.metadataSummary.readyCount}`,
  };
}

export function summarizeCmsBrowseCoreHealth(result: CmsBrowseLike) {
  const pageCount = countItems(result.pagesResult.data);

  return {
    pagesStatus: result.pagesResult.status,
    pageCount,
    cmsBrowseCoreFootprint: `pages:${result.pagesResult.status}:${pageCount}`,
  };
}

export function summarizeCmsDetailCoreHealth(result: CmsDetailLike) {
  const hasPage = Boolean(result.pageResult.data);
  const relatedSeedStatus = result.relatedPagesResult?.status ?? "unknown";
  const relatedSeedCount = countItems(result.relatedPagesResult?.data);

  return {
    pageStatus: result.pageResult.status,
    hasPage,
    relatedSeedStatus,
    relatedSeedCount,
    cmsDetailWorkflowFootprint: `page:${result.pageResult.status}:${hasPage ? "present" : "missing"}|related:${relatedSeedStatus}:${relatedSeedCount}`,
  };
}

export function summarizeMemberDashboardHealth(
  result: ResultWithStorefront & {
    identityContext: IdentityContextLike;
    commerceSummaryContext: CommerceSummaryLike;
    loyaltyBusinessesResult: { status: string; data?: { items?: unknown[] } | null };
  },
) {
  const addressCount = result.identityContext.addressesResult.data?.length ?? 0;
  const orderCount = countItems(result.commerceSummaryContext.ordersResult.data);
  const invoiceCount = countItems(result.commerceSummaryContext.invoicesResult.data);
  const loyaltyBusinessCount = countItems(result.loyaltyBusinessesResult.data);
  const storefrontSummary = summarizePublicStorefrontHealth(result.storefrontContext);

  return {
    ...storefrontSummary,
    profileStatus: result.identityContext.profileResult.status,
    preferencesStatus: result.identityContext.preferencesResult.status,
    customerContextStatus: result.identityContext.customerContextResult.status,
    addressesStatus: result.identityContext.addressesResult.status,
    addressCount,
    ordersStatus: result.commerceSummaryContext.ordersResult.status,
    orderCount,
    invoicesStatus: result.commerceSummaryContext.invoicesResult.status,
    invoiceCount,
    loyaltyStatus: result.commerceSummaryContext.loyaltyOverviewResult.status,
    loyaltyBusinessesStatus: result.loyaltyBusinessesResult.status,
    loyaltyBusinessCount,
    memberWorkflowFootprint: `orders:${result.commerceSummaryContext.ordersResult.status}|invoices:${result.commerceSummaryContext.invoicesResult.status}|loyalty:${result.commerceSummaryContext.loyaltyOverviewResult.status}|lanes:${storefrontSummary.promotionLaneFootprint}`,
    memberStorefrontContextFootprint: summarizeStorefrontContextFootprint(storefrontSummary),
  };
}

export function summarizeMemberEditorHealth(
  result: ResultWithStorefront & { identityContext: IdentityContextLike },
) {
  const addressCount = result.identityContext.addressesResult.data?.length ?? 0;
  const storefrontSummary = summarizePublicStorefrontHealth(result.storefrontContext);

  return {
    ...storefrontSummary,
    profileStatus: result.identityContext.profileResult.status,
    preferencesStatus: result.identityContext.preferencesResult.status,
    customerContextStatus: result.identityContext.customerContextResult.status,
    addressesStatus: result.identityContext.addressesResult.status,
    addressCount,
    memberWorkflowFootprint: `profile:${result.identityContext.profileResult.status}|preferences:${result.identityContext.preferencesResult.status}|addresses:${addressCount}|lanes:${storefrontSummary.promotionLaneFootprint}`,
    memberStorefrontContextFootprint: summarizeStorefrontContextFootprint(storefrontSummary),
  };
}

export function summarizeMemberCollectionHealth(
  result: ResultWithStorefront & {
    ordersResult?: { status: string; data?: { items?: unknown[] } | null };
    invoicesResult?: { status: string; data?: { items?: unknown[] } | null };
  },
) {
  const orderCount = countItems(result.ordersResult?.data);
  const invoiceCount = countItems(result.invoicesResult?.data);
  const ordersStatus = result.ordersResult?.status ?? "unknown";
  const invoicesStatus = result.invoicesResult?.status ?? "unknown";
  const storefrontSummary = summarizePublicStorefrontHealth(result.storefrontContext);

  return {
    ...storefrontSummary,
    ordersStatus,
    orderCount,
    invoicesStatus,
    invoiceCount,
    memberWorkflowFootprint: `orders:${ordersStatus}:${orderCount}|invoices:${invoicesStatus}:${invoiceCount}|lanes:${storefrontSummary.promotionLaneFootprint}`,
    memberStorefrontContextFootprint: summarizeStorefrontContextFootprint(storefrontSummary),
  };
}

export function summarizeMemberPagedCollectionHealth(
  result: MemberPagedCollectionLike,
) {
  const itemCount = countItems(result.data);
  const totalCount = result.data?.total ?? 0;
  const page = result.data?.request?.page ?? 1;
  const pageSize = result.data?.request?.pageSize ?? itemCount;

  return {
    status: result.status,
    itemCount,
    totalCount,
    page,
    pageSize,
    memberCollectionWorkflowFootprint: `status:${result.status}|page:${page}|size:${pageSize}|items:${itemCount}|total:${totalCount}`,
  };
}

export function summarizeMemberDetailHealth(result: MemberDetailLike) {
  const hasData = Boolean(result.data);

  return {
    status: result.status,
    hasData,
    memberWorkflowFootprint: `detail:${result.status}|has-data:${hasData ? "yes" : "no"}`,
  };
}

export function summarizeHomeDiscoveryHealth(result: {
  storefrontContext: PublicStorefrontContext;
  pagesResult: { status: string; data?: { items?: unknown[] } | null };
  categoriesResult: { status: string; data?: { items?: unknown[] } | null };
  productsResult: { status: string; data?: { items?: unknown[] } | null };
  categorySpotlights: Array<{ status: string }>;
}) {
  const storefrontSummary = summarizePublicStorefrontHealth(result.storefrontContext);
  const homePageCount = countItems(result.pagesResult.data);
  const homeCategoryCount = countItems(result.categoriesResult.data);
  const homeProductCount = countItems(result.productsResult.data);
  const spotlightCount = result.categorySpotlights.length;
  const degradedSpotlightCount = result.categorySpotlights.filter(
    (spotlight) => spotlight.status !== "ok",
  ).length;

  return {
    ...storefrontSummary,
    homePagesStatus: result.pagesResult.status,
    homePageCount,
    homeCategoriesStatus: result.categoriesResult.status,
    homeCategoryCount,
    homeProductsStatus: result.productsResult.status,
    homeProductCount,
    spotlightCount,
    degradedSpotlightCount,
    homeDiscoveryWorkflowFootprint: `pages:${result.pagesResult.status}:${homePageCount}|categories:${result.categoriesResult.status}:${homeCategoryCount}|products:${result.productsResult.status}:${homeProductCount}|spotlights:${spotlightCount}|degraded:${degradedSpotlightCount}`,
  };
}

export function summarizeHomeRouteHealth(result: {
  memberSession: unknown | null;
  homeDiscoveryContext: {
    storefrontContext: PublicStorefrontContext;
  };
  parts: unknown[];
}) {
  const storefrontSummary = summarizePublicStorefrontHealth(result.homeDiscoveryContext.storefrontContext);

  return {
    memberSessionState: result.memberSession ? "present" : "missing",
    partCount: result.parts.length,
    ...storefrontSummary,
    homeWorkflowFootprint: `session:${result.memberSession ? "present" : "missing"}|parts:${result.parts.length}|cart:${storefrontSummary.cartStatus}|lanes:${storefrontSummary.promotionLaneFootprint}`,
    homeStorefrontContextFootprint: summarizeStorefrontContextFootprint(storefrontSummary),
  };
}

export function summarizeHomeCategorySpotlightsHealth(
  result: HomeCategorySpotlightResultLike,
) {
  const degradedSpotlightCount = result.filter(
    (entry) => entry.categoryProductsResult.status !== "ok",
  ).length;
  const spotlightProductCount = result.reduce(
    (total, entry) => total + countItems(entry.categoryProductsResult.data),
    0,
  );

  return {
    spotlightCount: result.length,
    degradedSpotlightCount,
    spotlightProductCount,
    homeCategorySpotlightsFootprint: `spotlights:${result.length}|degraded:${degradedSpotlightCount}|products:${spotlightProductCount}`,
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
  const storefrontSummary = summarizePublicStorefrontHealth(result.storefrontContext);

  return {
    ...storefrontSummary,
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
  const lineCount = result.data?.lines?.length ?? 0;
  const paymentCount = result.data?.payments?.length ?? 0;
  const paidPaymentCount =
    result.data?.payments?.filter((payment) => payment.status === "Paid")
      .length ?? 0;

  return {
    status: result.status,
    lineCount,
    paymentCount,
    paidPaymentCount,
    confirmationWorkflowFootprint: `status:${result.status}|lines:${lineCount}|payments:${paymentCount}|paid:${paidPaymentCount}`,
  };
}

export function summarizeCartPageHealth(result: {
  routeContext: {
    model: CartViewModelLike;
    memberSession?: unknown | null;
    identityContext?: IdentityContextLike | null;
  };
  followUpProducts: ProductPromotionSummaryLike[];
}) {
  const memberSessionState = result.routeContext.memberSession ? "present" : "missing";
  const addressesStatus =
    result.routeContext.identityContext?.addressesResult.status ?? "unauthenticated";
  const promotionSummary = summarizePromotionLaneHealth(result.followUpProducts);

  return {
    cartStatus: result.routeContext.model.status,
    cartItemCount: result.routeContext.model.cart?.items.length ?? 0,
    memberSessionState,
    addressesStatus,
    followUpProductCount: result.followUpProducts.length,
    ...promotionSummary,
    commerceWorkflowFootprint: `surface:cart|session:${memberSessionState}|addresses:${addressesStatus}|lanes:${promotionSummary.promotionLaneFootprint}`,
    commerceStorefrontContextFootprint: `products:${result.followUpProducts.length}|cart:${result.routeContext.model.status}|addresses:${addressesStatus}`,
  };
}

export function summarizeCheckoutPageHealth(result: {
  routeContext: {
    model: CartViewModelLike;
    memberSession?: unknown | null;
    identityContext?: IdentityContextLike | null;
    commerceSummaryContext?: CommerceSummaryLike | null;
    storefrontContext: PublicStorefrontContext;
  };
}) {
  const memberSessionState = result.routeContext.memberSession ? "present" : "missing";
  const addressesStatus =
    result.routeContext.identityContext?.addressesResult.status ?? "unauthenticated";
  const invoicesStatus =
    result.routeContext.commerceSummaryContext?.invoicesResult.status ??
    "unauthenticated";
  const storefrontSummary = summarizePublicStorefrontHealth(
    result.routeContext.storefrontContext,
  );
  const promotionSummary = summarizePromotionLaneHealth(
    result.routeContext.storefrontContext.products,
  );

  return {
    cartStatus: result.routeContext.model.status,
    cartItemCount: result.routeContext.model.cart?.items.length ?? 0,
    memberSessionState,
    addressesStatus,
    invoicesStatus,
    invoiceCount: countItems(
      result.routeContext.commerceSummaryContext?.invoicesResult.data,
    ),
    ...promotionSummary,
    commerceWorkflowFootprint: `surface:checkout|session:${memberSessionState}|addresses:${addressesStatus}|invoices:${invoicesStatus}|lanes:${promotionSummary.promotionLaneFootprint}`,
    commerceStorefrontContextFootprint: summarizeStorefrontContextFootprint(storefrontSummary),
  };
}

export function summarizeConfirmationPageHealth(result: {
  routeContext: {
    confirmationResult: ConfirmationResultLike;
    memberSession?: unknown | null;
    commerceSummaryContext?: CommerceSummaryLike | null;
  };
  followUpProducts: ProductPromotionSummaryLike[];
}) {
  const memberSessionState = result.routeContext.memberSession ? "present" : "missing";
  const ordersStatus =
    result.routeContext.commerceSummaryContext?.ordersResult.status ??
    "unauthenticated";
  const invoicesStatus =
    result.routeContext.commerceSummaryContext?.invoicesResult.status ??
    "unauthenticated";
  const promotionSummary = summarizePromotionLaneHealth(result.followUpProducts);

  return {
    confirmationStatus: result.routeContext.confirmationResult.status,
    lineCount: result.routeContext.confirmationResult.data?.lines?.length ?? 0,
    memberSessionState,
    ordersStatus,
    invoicesStatus,
    followUpProductCount: result.followUpProducts.length,
    ...promotionSummary,
    commerceWorkflowFootprint: `surface:confirmation|session:${memberSessionState}|orders:${ordersStatus}|invoices:${invoicesStatus}|lanes:${promotionSummary.promotionLaneFootprint}`,
    confirmationFollowUpWorkflowFootprint: `products:${result.followUpProducts.length}|lanes:${promotionSummary.promotionLaneFootprint}`,
    commerceStorefrontContextFootprint: `products:${result.followUpProducts.length}|orders:${ordersStatus}|invoices:${invoicesStatus}`,
  };
}
export function summarizeStorefrontShoppingHealth(result: ShoppingContextLike) {
  const anonymousCartState = result.anonymousCartId ? "present" : "missing";
  const liveCartItemCount = result.cartResult.data?.items?.length ?? 0;

  return {
    anonymousCartState,
    liveCartStatus: result.cartResult.status,
    liveCartItemCount,
    snapshotCount: result.cartSnapshots.length,
    cartLinkedCount: result.cartLinkedProductSlugs.length,
    storefrontShoppingFootprint: `anonymous:${anonymousCartState}|live:${result.cartResult.status}:${liveCartItemCount}|snapshots:${result.cartSnapshots.length}|linked:${result.cartLinkedProductSlugs.length}`,
  };
}

export function summarizeShellHealth(result: ShellMenuLike) {
  const menuItemCount = result.data?.items?.length ?? 0;

  return {
    menuStatus: result.status,
    menuItemCount,
    shellMenuFootprint: `status:${result.status}|items:${menuItemCount}`,
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
  const primaryNavigationCount = result.primaryNavigation.length;
  const utilityLinkCount = result.utilityLinks.length;
  const footerGroupCount = result.footerGroups.length;

  return {
    culture: result.culture,
    menuSource: result.menuSource,
    menuStatus: result.menuStatus,
    primaryNavigationCount,
    utilityLinkCount,
    footerGroupCount,
    shellModelFootprint: `culture:${result.culture}|source:${result.menuSource}|menu:${result.menuStatus}|primary:${primaryNavigationCount}|utility:${utilityLinkCount}|footer:${footerGroupCount}`,
  };
}

export function summarizeCartViewModelHealth(result: CartViewModelLike) {
  const anonymousCartState = result.anonymousId ? "present" : "missing";
  const cartItemCount = result.cart?.items.length ?? 0;
  const hasCoupon = Boolean(result.cart?.couponCode);

  return {
    anonymousCartState,
    cartStatus: result.status,
    cartItemCount,
    hasCoupon,
    cartModelFootprint: `anonymous:${anonymousCartState}|status:${result.status}|items:${cartItemCount}|coupon:${hasCoupon ? "yes" : "no"}`,
  };
}

export function summarizeMemberIdentityHealth(result: IdentityContextLike) {
  const addressCount = result.addressesResult.data?.length ?? 0;

  return {
    profileStatus: result.profileResult.status,
    preferencesStatus: result.preferencesResult.status,
    customerContextStatus: result.customerContextResult.status,
    addressesStatus: result.addressesResult.status,
    addressCount,
    memberIdentityFootprint: `profile:${result.profileResult.status}|preferences:${result.preferencesResult.status}|customer:${result.customerContextResult.status}|addresses:${result.addressesResult.status}:${addressCount}`,
  };
}

export function summarizePublicAuthRouteHealth(result: {
  storefrontContext: PublicStorefrontContext;
  route?: string;
}) {
  const storefrontSummary = summarizePublicStorefrontHealth(result.storefrontContext);

  return {
    ...storefrontSummary,
    authEntryWorkflowFootprint: `route:${result.route ?? "unknown"}|cart:${storefrontSummary.cartStatus}|lanes:${storefrontSummary.promotionLaneFootprint}`,
    authEntryStorefrontContextFootprint: summarizeStorefrontContextFootprint(storefrontSummary),
  };
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
    const dashboardSummary = summarizeMemberDashboardHealth({
      storefrontContext: result.memberRouteContext.storefrontContext,
      identityContext: result.memberRouteContext.identityContext,
      commerceSummaryContext: result.memberRouteContext.commerceSummaryContext,
      loyaltyBusinessesResult: result.memberRouteContext.loyaltyBusinessesResult,
    });

    return {
      sessionState: "present",
      ...dashboardSummary,
      accountWorkflowFootprint: `surface:member|orders:${dashboardSummary.ordersStatus}|invoices:${dashboardSummary.invoicesStatus}|lanes:${dashboardSummary.promotionLaneFootprint}`,
      accountStorefrontContextFootprint: dashboardSummary.memberStorefrontContextFootprint,
    };
  }

  const storefrontSummary = result.publicRouteContext
    ? summarizePublicStorefrontHealth(result.publicRouteContext.storefrontContext)
    : null;

  return {
    sessionState: "missing",
    ...(storefrontSummary ?? {}),
    accountWorkflowFootprint: storefrontSummary
      ? `surface:public|cart:${storefrontSummary.cartStatus}|lanes:${storefrontSummary.promotionLaneFootprint}`
      : "surface:public|storefront:missing",
    accountStorefrontContextFootprint: storefrontSummary
      ? summarizeStorefrontContextFootprint(storefrontSummary)
      : "storefront:missing",
  };
}

export function summarizeProtectedMemberEntryHealth(result: {
  session: unknown | null;
  storefrontContext: PublicStorefrontContext | null;
}) {
  const sessionState = result.session ? "present" : "missing";
  const storefrontSummary = result.storefrontContext
    ? summarizePublicStorefrontHealth(result.storefrontContext)
    : null;

  return {
    sessionState,
    ...(storefrontSummary ?? {}),
    memberEntryWorkflowFootprint: storefrontSummary
      ? `session:${sessionState}|cart:${storefrontSummary.cartStatus}|lanes:${storefrontSummary.promotionLaneFootprint}`
      : `session:${sessionState}|storefront:missing`,
    memberEntryStorefrontContextFootprint: storefrontSummary
      ? summarizeStorefrontContextFootprint(storefrontSummary)
      : "storefront:missing",
  };
}

export function summarizeMemberCommerceSummaryHealth(
  result: CommerceSummaryLike,
) {
  const orderCount = countItems(result.ordersResult.data);
  const invoiceCount = countItems(result.invoicesResult.data);

  return {
    ordersStatus: result.ordersResult.status,
    orderCount,
    invoicesStatus: result.invoicesResult.status,
    invoiceCount,
    loyaltyStatus: result.loyaltyOverviewResult.status,
    memberCommerceSummaryFootprint: `orders:${result.ordersResult.status}:${orderCount}|invoices:${result.invoicesResult.status}:${invoiceCount}|loyalty:${result.loyaltyOverviewResult.status}`,
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














































