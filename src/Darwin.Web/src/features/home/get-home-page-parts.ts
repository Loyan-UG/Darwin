import "server-only";
import { getCatalogReviewTargets } from "@/features/catalog/discovery";
import { getProductSavingsPercent } from "@/features/catalog/merchandising";
import { getPendingCmsReviewTargets, isDiscoveryReadyPage } from "@/features/cms/discovery";
import {
  buildStorefrontCategorySpotlightCards,
  buildStorefrontOfferCards,
} from "@/features/storefront/storefront-campaigns";
import { buildStorefrontSpotlightSelections } from "@/features/storefront/storefront-spotlight";
import type { MemberSession } from "@/features/member-session/types";
import {
  getMemberCommerceSummaryContext,
  getMemberIdentityContext,
} from "@/features/member-portal/server/get-member-summary-context";
import {
  buildCheckoutDraftSearch,
  toCheckoutDraftFromMemberAddress,
} from "@/features/checkout/helpers";
import { getHomeDiscoveryContext } from "@/features/home/server/get-home-discovery-context";
import {
  buildCatalogReviewTargetHref,
  buildCmsReviewTargetHref,
} from "@/features/review/review-window";
import {
  buildPreferredCatalogReviewWindowHref,
  buildPreferredCmsReviewWindowHref,
  getCatalogReviewQueueState,
  getCmsReviewQueueState,
  getPreferredCatalogReviewState,
  getPreferredCmsReviewState,
} from "@/features/review/review-workflow";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { buildAppQueryPath } from "@/lib/locale-routing";
import { getSupportedCultures } from "@/lib/request-culture";
import {
  formatResource,
  getHomeResource,
  resolveLocalizedQueryMessage,
} from "@/localization";
import type { WebPagePart } from "@/web-parts/types";

export async function getHomePageParts(
  culture: string,
  session?: MemberSession | null,
): Promise<WebPagePart[]> {
  const copy = getHomeResource(culture);
  const supportedCultures = getSupportedCultures();
  const [homeDiscoveryContext, memberCommerceContext, memberIdentityContext] = await Promise.all([
    getHomeDiscoveryContext(culture),
    session
      ? getMemberCommerceSummaryContext()
      : Promise.resolve(null),
    session ? getMemberIdentityContext() : Promise.resolve(null),
  ]);
  const { pagesResult, productsResult, categoriesResult, categorySpotlights, storefrontContext } =
    homeDiscoveryContext;
  const cartSnapshots = storefrontContext.cartSnapshots.slice(0, 3);
  const cartResult = {
    data: storefrontContext.storefrontCart,
    status: storefrontContext.storefrontCartStatus,
  };
  const cartLinkedSlugs = new Set(storefrontContext.cartLinkedProductSlugs);
  const cmsHealthy = pagesResult.status === "ok";
  const catalogHealthy = productsResult.status === "ok";
  const categoriesHealthy = categoriesResult.status === "ok";
  const resolvedPagesMessage = resolveLocalizedQueryMessage(pagesResult.message, copy);
  const resolvedProductsMessage = resolveLocalizedQueryMessage(productsResult.message, copy);
  const resolvedCategoriesMessage = resolveLocalizedQueryMessage(categoriesResult.message, copy);
  const resolvedOrdersMessage = resolveLocalizedQueryMessage(
    memberCommerceContext?.ordersResult.message,
    copy,
  );
  const resolvedInvoicesMessage = resolveLocalizedQueryMessage(
    memberCommerceContext?.invoicesResult.message,
    copy,
  );
  const resolvedLoyaltyMessage = resolveLocalizedQueryMessage(
    memberCommerceContext?.loyaltyOverviewResult.message,
    copy,
  );
  const readyCmsPagesCount = (pagesResult.data?.items ?? []).filter((page) =>
    isDiscoveryReadyPage(page),
  ).length;
  const cmsAttentionCount = Math.max(
    (pagesResult.data?.items.length ?? 0) - readyCmsPagesCount,
    0,
  );
  const spotlightPage = pagesResult.data?.items[0];
  const spotlightSelections = buildStorefrontSpotlightSelections({
    cmsPages: pagesResult.data?.items ?? [],
    categories: categoriesResult.data?.items ?? [],
    products: productsResult.data?.items ?? [],
    offerBoardCount: 3,
  });
  const rankedProducts = spotlightSelections.rankedProducts;
  const spotlightProduct = spotlightSelections.spotlightProduct;
  const visibleOfferCount = rankedProducts.filter(
    (product) => getProductSavingsPercent(product) !== null,
  ).length;
  const visibleBaseCount = Math.max(rankedProducts.length - visibleOfferCount, 0);
  const preferredCmsReviewState = getPreferredCmsReviewState(
    readyCmsPagesCount,
    cmsAttentionCount,
  );
  const cmsReviewWindowHref = buildPreferredCmsReviewWindowHref(
    preferredCmsReviewState,
  );
  const preferredCatalogReviewState = getPreferredCatalogReviewState(
    visibleOfferCount,
    visibleBaseCount,
  );
  const catalogReviewWindowHref = buildPreferredCatalogReviewWindowHref(
    preferredCatalogReviewState,
  );
  const cmsReviewTargets = getCmsReviewQueueState(
    getPendingCmsReviewTargets(pagesResult.data?.items ?? []),
    { previewCount: 2 },
  ).previewTargets;
  const catalogReviewTargets = getCatalogReviewQueueState(
    getCatalogReviewTargets(rankedProducts),
    { previewCount: 2 },
  ).previewTargets;
  const offerBoardProducts =
    (cartLinkedSlugs.size > 0
      ? rankedProducts.filter((product) => !cartLinkedSlugs.has(product.slug))
      : rankedProducts
    ).slice(0, 3);
  const recentOrders = memberCommerceContext?.ordersResult.data?.items.slice(0, 2) ?? [];
  const recentInvoices = memberCommerceContext?.invoicesResult.data?.items.slice(0, 2) ?? [];
  const memberAddresses = memberIdentityContext?.addressesResult.data ?? [];
  const preferredMemberAddress =
    memberAddresses.find((address) => address.isDefaultShipping) ??
    memberAddresses.find((address) => address.isDefaultBilling) ??
    memberAddresses[0] ??
    null;
  const memberCheckoutHref = preferredMemberAddress
    ? `/checkout${buildCheckoutDraftSearch(
        toCheckoutDraftFromMemberAddress(preferredMemberAddress),
        { memberAddressId: preferredMemberAddress.id },
      )}`
    : "/account/addresses";
  const invoiceAttention =
    recentInvoices.find((invoice) => invoice.balanceMinor > 0) ??
    recentInvoices[0] ??
    null;
  const loyaltyFocusAccount =
    [...(memberCommerceContext?.loyaltyOverviewResult.data?.accounts ?? [])].sort((left, right) => {
      const leftRank = left.pointsToNextReward ?? Number.MAX_SAFE_INTEGER;
      const rightRank = right.pointsToNextReward ?? Number.MAX_SAFE_INTEGER;
      return leftRank - rightRank;
    })[0] ?? null;
  const homePriorityItems = [
    ...(cartResult.data && cartResult.data.items.length > 0
      ? [
          {
            id: "home-priority-checkout",
            title: copy.priorityCheckoutTitle,
            description: formatResource(copy.priorityCheckoutDescription, {
              itemCount: cartResult.data.items.length,
              total: formatMoney(
                cartResult.data.grandTotalGrossMinor,
                cartResult.data.currency,
                culture,
              ),
            }),
            href: "/checkout",
            ctaLabel: copy.priorityCheckoutCta,
            meta: formatResource(copy.priorityCheckoutMeta, {
              status: cartResult.status,
            }),
          },
        ]
      : []),
    ...(invoiceAttention
      ? [
          {
            id: `home-priority-invoice-${invoiceAttention.id}`,
            title: formatResource(copy.priorityInvoiceTitle, {
              reference: invoiceAttention.orderNumber ?? invoiceAttention.id,
            }),
            description: formatResource(copy.priorityInvoiceDescription, {
              balance: formatMoney(
                invoiceAttention.balanceMinor,
                invoiceAttention.currency,
                culture,
              ),
              status: invoiceAttention.status,
            }),
            href: `/invoices/${invoiceAttention.id}`,
            ctaLabel: copy.priorityInvoiceCta,
            meta: formatResource(copy.priorityInvoiceMeta, {
              status: memberCommerceContext?.invoicesResult.status ?? "ok",
            }),
          },
        ]
      : []),
    ...(loyaltyFocusAccount
      ? [
          {
            id: `home-priority-loyalty-${loyaltyFocusAccount.loyaltyAccountId}`,
            title: formatResource(copy.priorityLoyaltyTitle, {
              business: loyaltyFocusAccount.businessName,
            }),
            description: formatResource(copy.priorityLoyaltyDescription, {
              reward:
                loyaltyFocusAccount.nextRewardTitle ??
                copy.memberResumeUnavailable,
              points:
                loyaltyFocusAccount.pointsToNextReward?.toString() ??
                copy.memberResumeUnavailable,
            }),
            href: `/loyalty/${loyaltyFocusAccount.businessId}`,
            ctaLabel: copy.priorityLoyaltyCta,
            meta: formatResource(copy.priorityLoyaltyMeta, {
              status: memberCommerceContext?.loyaltyOverviewResult.status ?? "ok",
            }),
          },
        ]
      : []),
    ...(recentOrders.length > 0
      ? [
          {
            id: `home-priority-order-${recentOrders[0]!.id}`,
            title: formatResource(copy.priorityOrderTitle, {
              orderNumber: recentOrders[0]!.orderNumber,
            }),
            description: formatResource(copy.priorityOrderDescription, {
              createdAt: formatDateTime(recentOrders[0]!.createdAtUtc, culture),
              total: formatMoney(
                recentOrders[0]!.grandTotalGrossMinor,
                recentOrders[0]!.currency,
                culture,
              ),
            }),
            href: `/orders/${recentOrders[0]!.id}`,
            ctaLabel: copy.priorityOrderCta,
            meta: formatResource(copy.priorityOrderMeta, {
              status: memberCommerceContext?.ordersResult.status ?? "ok",
            }),
          },
        ]
      : []),
    ...(spotlightProduct
      ? [
          {
            id: `home-priority-product-${spotlightProduct.id}`,
            title: formatResource(copy.priorityProductTitle, {
              product: spotlightProduct.name,
            }),
            description: formatResource(copy.priorityProductDescription, {
              price: formatMoney(
                spotlightProduct.priceMinor,
                spotlightProduct.currency,
                culture,
              ),
            }),
            href: `/catalog/${spotlightProduct.slug}`,
            ctaLabel: copy.priorityProductCta,
            meta: formatResource(copy.priorityProductMeta, {
              status: productsResult.status,
            }),
          },
        ]
      : []),
    ...(spotlightPage
      ? [
          {
            id: `home-priority-page-${spotlightPage.id}`,
            title: formatResource(copy.priorityPageTitle, {
              title: spotlightPage.title,
            }),
            description:
              spotlightPage.metaDescription ?? copy.priorityPageFallbackDescription,
            href: `/cms/${spotlightPage.slug}`,
            ctaLabel: copy.priorityPageCta,
            meta: formatResource(copy.priorityPageMeta, {
              status: pagesResult.status,
            }),
          },
        ]
      : []),
  ].slice(0, 4);
  const strongestCategoryOpportunity =
    categorySpotlights.find((entry) => entry.product && entry.status === "ok") ??
    categorySpotlights[0] ??
    null;
  const categoryCampaignCards = buildStorefrontCategorySpotlightCards(
    categorySpotlights,
    {
      eyebrow: copy.campaignBoardCardEyebrow,
      ctaLabel: copy.campaignBoardCta,
      formatPrice: (product) =>
        formatMoney(product.priceMinor, product.currency, culture),
      formatProductDescription: (entry, input) =>
        input.savingsPercent !== null
          ? formatResource(copy.campaignBoardProductDescription, {
              product: entry.product!.name,
              savings: input.savingsPercent,
              price: input.price,
            })
          : formatResource(copy.campaignBoardFallbackProductDescription, {
              product: entry.product!.name,
              price: input.price,
            }),
      formatFallbackDescription: (entry) =>
        formatResource(copy.campaignBoardCategoryFallbackDescription, {
          status: entry.status,
        }),
      formatMeta: (entry) =>
        entry.product && entry.status === "ok"
          ? formatResource(copy.campaignBoardProductMeta, {
              compareAt:
                typeof entry.product.compareAtPriceMinor === "number" &&
                (getProductSavingsPercent(entry.product) ?? 0) > 0
                  ? formatMoney(
                      entry.product.compareAtPriceMinor,
                      entry.product.currency,
                      culture,
                    )
                  : copy.campaignBoardProductMetaFallback,
            })
          : formatResource(copy.campaignBoardCategoryMeta, {
              status: entry.status,
            }),
    },
  );
  const offerBoardCards = buildStorefrontOfferCards(offerBoardProducts, {
    labels: {
      heroOffer: copy.offerCampaignHeroLabel,
      valueOffer: copy.offerCampaignValueLabel,
      priceDrop: copy.offerCampaignPriceDropLabel,
      steadyPick: copy.offerCampaignSteadyLabel,
    },
    formatPrice: (product) =>
      formatMoney(product.priceMinor, product.currency, culture),
    describeWithSavings: (_, input) =>
      formatResource(copy.offerBoardOfferDescription, {
        savings: input.savingsPercent,
        price: input.price,
      }),
    describeWithoutSavings: (_, input) =>
      formatResource(copy.offerBoardOfferFallbackDescription, {
        price: input.price,
      }),
    fallbackDescription: copy.offerBoardOfferFallbackDescription,
    formatMeta: (product) =>
      getProductSavingsPercent(product) !== null
        ? formatResource(copy.offerBoardOfferMeta, {
            compareAt: formatMoney(
              product.compareAtPriceMinor ?? product.priceMinor,
              product.currency,
              culture,
            ),
          })
        : copy.offerBoardOfferFallbackMeta,
    ctaLabel: copy.offerBoardCta,
  });

  return [
    {
      id: "home-hero",
      kind: "hero",
      eyebrow: copy.heroEyebrow,
      title: copy.heroTitle,
      description: copy.heroDescription,
      actions: [
        { label: copy.browseCatalogCta, href: "/catalog" },
        { label: copy.openCheckoutCta, href: "/checkout", variant: "secondary" },
      ],
      highlights: [
        copy.heroHighlightTheme,
        copy.heroHighlightComposition,
        formatResource(copy.heroHighlightStatus, {
          pagesStatus: pagesResult.status,
          productsStatus: productsResult.status,
        }),
        formatResource(copy.heroHighlightCategoriesStatus, {
          categoriesStatus: categoriesResult.status,
        }),
      ],
      panelTitle: copy.heroPanelTitle,
    },
    {
      id: "home-metrics",
      kind: "stat-grid",
      eyebrow: copy.metricsEyebrow,
      title: copy.metricsTitle,
      description: copy.metricsDescription,
      metrics: [
        {
          id: "metric-pages",
          label: copy.metricPagesLabel,
          value: String(pagesResult.data?.total ?? pagesResult.data?.items.length ?? 0),
          note: cmsHealthy
            ? copy.metricPagesNote
            : formatResource(copy.metricPagesDegradedNote, {
                status: pagesResult.status,
              }),
        },
        {
          id: "metric-products",
          label: copy.metricProductsLabel,
          value: String(
            productsResult.data?.total ?? productsResult.data?.items.length ?? 0,
          ),
          note: catalogHealthy
            ? copy.metricProductsNote
            : formatResource(copy.metricProductsDegradedNote, {
                status: productsResult.status,
              }),
        },
        {
          id: "metric-categories",
          label: copy.metricCategoriesLabel,
          value: String(
            categoriesResult.data?.total ??
              categoriesResult.data?.items.length ??
              0,
          ),
          note: categoriesHealthy
            ? copy.metricCategoriesNote
            : formatResource(copy.metricCategoriesDegradedNote, {
                status: categoriesResult.status,
              }),
        },
        {
          id: "metric-cultures",
          label: copy.metricCulturesLabel,
          value: String(supportedCultures.length),
          note: copy.metricCulturesNote,
        },
      ],
    },
    {
      id: "home-browse-readiness",
      kind: "status-list",
      eyebrow: copy.discoveryReadinessEyebrow,
      title: copy.discoveryReadinessTitle,
      description: copy.discoveryReadinessDescription,
      items: [
        {
          id: "home-browse-readiness-cms",
          label: copy.discoveryReadinessCmsLabel,
          title: copy.discoveryReadinessCmsTitle,
          description: formatResource(copy.discoveryReadinessCmsDescription, {
            readyCount: readyCmsPagesCount,
            attentionCount: cmsAttentionCount,
          }),
          href: "/cms",
          ctaLabel: copy.discoveryReadinessCmsCta,
          tone: cmsHealthy ? "ok" : "warning",
          meta: formatResource(copy.discoveryReadinessCmsMeta, {
            status: pagesResult.status,
          }),
        },
        {
          id: "home-browse-readiness-catalog",
          label: copy.discoveryReadinessCatalogLabel,
          title: copy.discoveryReadinessCatalogTitle,
          description: formatResource(copy.discoveryReadinessCatalogDescription, {
            offerCount: visibleOfferCount,
            baseCount: visibleBaseCount,
            categoryCount: categoriesResult.data?.items.length ?? 0,
          }),
          href: "/catalog",
          ctaLabel: copy.discoveryReadinessCatalogCta,
          tone: catalogHealthy && categoriesHealthy ? "ok" : "warning",
          meta: formatResource(copy.discoveryReadinessCatalogMeta, {
            productsStatus: productsResult.status,
            categoriesStatus: categoriesResult.status,
          }),
        },
      ],
      emptyMessage: copy.discoveryReadinessEmptyMessage,
    },
    {
      id: "home-review-targets",
      kind: "link-list",
      eyebrow: copy.reviewTargetsEyebrow,
      title: copy.reviewTargetsTitle,
      description: copy.reviewTargetsDescription,
      items: [
        {
          id: "home-review-cms-window",
          title: copy.reviewTargetsCmsWindowTitle,
          description: formatResource(copy.reviewTargetsCmsWindowDescription, {
            readyCount: readyCmsPagesCount,
            attentionCount: cmsAttentionCount,
          }),
          href: cmsReviewWindowHref,
          ctaLabel:
            preferredCmsReviewState === "needs-attention"
              ? copy.reviewTargetsCmsAttentionCta
              : copy.reviewTargetsCmsReadyCta,
          meta: formatResource(copy.reviewTargetsCmsWindowMeta, {
            status: pagesResult.status,
          }),
        },
        ...cmsReviewTargets.map(({ page, missingMetaTitle, missingMetaDescription }) => ({
          id: `home-review-cms-target-${page.id}`,
          title: formatResource(copy.reviewTargetsCmsTargetTitle, {
            title: page.title,
          }),
          description:
            missingMetaTitle && missingMetaDescription
              ? copy.reviewTargetsCmsTargetBothDescription
              : missingMetaTitle
                ? copy.reviewTargetsCmsTargetMetaTitleDescription
                : copy.reviewTargetsCmsTargetMetaDescriptionDescription,
          href: buildCmsReviewTargetHref(page.slug, {
            visibleState:
              missingMetaTitle || missingMetaDescription
                ? "needs-attention"
                : "ready",
            visibleSort:
              missingMetaTitle || missingMetaDescription
                ? "attention-first"
                : "ready-first",
          }),
          ctaLabel: copy.reviewTargetsCmsTargetCta,
          meta: page.slug,
        })),
        {
          id: "home-review-catalog-window",
          title: copy.reviewTargetsCatalogWindowTitle,
          description: formatResource(copy.reviewTargetsCatalogWindowDescription, {
            offerCount: visibleOfferCount,
            baseCount: visibleBaseCount,
          }),
          href: catalogReviewWindowHref,
          ctaLabel:
            preferredCatalogReviewState === "offers"
              ? copy.reviewTargetsCatalogOffersCta
              : copy.reviewTargetsCatalogBaseCta,
          meta: formatResource(copy.reviewTargetsCatalogWindowMeta, {
            status: productsResult.status,
          }),
        },
        ...catalogReviewTargets.map(({ product, missingImage, savingsAmount }) => ({
          id: `home-review-catalog-target-${product.id}`,
          title: formatResource(copy.reviewTargetsCatalogTargetTitle, {
            product: product.name,
          }),
          description: missingImage
            ? copy.reviewTargetsCatalogTargetImageDescription
            : savingsAmount > 0
              ? formatResource(copy.reviewTargetsCatalogTargetOfferDescription, {
                  savings: getProductSavingsPercent(product) ?? 0,
                })
              : copy.reviewTargetsCatalogTargetBaseDescription,
          href: buildCatalogReviewTargetHref(product.slug, {
            visibleState: missingImage || savingsAmount > 0 ? "offers" : "base",
            visibleSort:
              missingImage || savingsAmount > 0 ? "offers-first" : "base-first",
          }),
          ctaLabel: copy.reviewTargetsCatalogTargetCta,
          meta: formatMoney(product.priceMinor, product.currency, culture),
        })),
      ],
      emptyMessage: copy.reviewTargetsEmptyMessage,
    },
    {
      id: "home-priority-lane",
      kind: "link-list",
      eyebrow: copy.priorityEyebrow,
      title: copy.priorityTitle,
      description: copy.priorityDescription,
      items: homePriorityItems,
      emptyMessage: copy.priorityEmptyMessage,
    },
    {
      id: "home-commerce-opportunity",
      kind: "status-list",
      eyebrow: copy.commerceOpportunityEyebrow,
      title: copy.commerceOpportunityTitle,
      description: copy.commerceOpportunityDescription,
      items: [
        ...(cartResult.data && cartResult.data.items.length > 0
          ? [
              {
                id: "commerce-opportunity-cart",
                label: copy.commerceOpportunityCartLabel,
                title: copy.commerceOpportunityCartTitle,
                description: formatResource(
                  copy.commerceOpportunityCartDescription,
                  {
                    itemCount: cartResult.data.items.length,
                    total: formatMoney(
                      cartResult.data.grandTotalGrossMinor,
                      cartResult.data.currency,
                      culture,
                    ),
                  },
                ),
                href: "/checkout",
                ctaLabel: copy.commerceOpportunityCartCta,
                tone: "ok" as const,
                meta: formatResource(copy.commerceOpportunityCartMeta, {
                  status: cartResult.status,
                }),
              },
            ]
          : []),
        ...(spotlightProduct
          ? [
              {
                id: `commerce-opportunity-product-${spotlightProduct.id}`,
                label: copy.commerceOpportunityProductLabel,
                title: formatResource(copy.commerceOpportunityProductTitle, {
                  product: spotlightProduct.name,
                }),
                description: formatResource(
                  copy.commerceOpportunityProductDescription,
                  {
                    price: formatMoney(
                      spotlightProduct.priceMinor,
                      spotlightProduct.currency,
                      culture,
                    ),
                  },
                ),
                href: `/catalog/${spotlightProduct.slug}`,
                ctaLabel: copy.commerceOpportunityProductCta,
                tone: catalogHealthy ? ("ok" as const) : ("warning" as const),
                meta: formatResource(copy.commerceOpportunityProductMeta, {
                  status: productsResult.status,
                }),
              },
            ]
          : []),
        ...(strongestCategoryOpportunity
          ? [
              {
                id: `commerce-opportunity-category-${strongestCategoryOpportunity.category.id}`,
                label: copy.commerceOpportunityCategoryLabel,
                title: formatResource(copy.commerceOpportunityCategoryTitle, {
                  category: strongestCategoryOpportunity.category.name,
                }),
                description:
                  strongestCategoryOpportunity.product &&
                  strongestCategoryOpportunity.status === "ok"
                    ? formatResource(
                        copy.commerceOpportunityCategoryDescription,
                        {
                          product:
                            strongestCategoryOpportunity.product.name,
                        },
                      )
                    : formatResource(
                        copy.commerceOpportunityCategoryFallbackDescription,
                        {
                          status: strongestCategoryOpportunity.status,
                        },
                      ),
                href: buildAppQueryPath("/catalog", {
                  category: strongestCategoryOpportunity.category.slug,
                }),
                ctaLabel: copy.commerceOpportunityCategoryCta,
                tone:
                  strongestCategoryOpportunity.product &&
                  strongestCategoryOpportunity.status === "ok"
                    ? ("ok" as const)
                    : ("warning" as const),
                meta: formatResource(copy.commerceOpportunityCategoryMeta, {
                  status: strongestCategoryOpportunity.status,
                }),
              },
            ]
          : []),
      ],
      emptyMessage: copy.commerceOpportunityEmptyMessage,
    },
    {
      id: "home-offer-board",
      kind: "card-grid",
      eyebrow: copy.offerBoardEyebrow,
      title: copy.offerBoardTitle,
      description:
        cartLinkedSlugs.size > 0
          ? formatResource(copy.offerBoardCartAwareDescription, {
              count: cartLinkedSlugs.size,
            })
          : copy.offerBoardDescription,
      cards: offerBoardCards.map((product) => ({
        id: `offer-board-${product.id}`,
        eyebrow: copy.offerBoardOfferEyebrow,
        title: product.title,
        description: product.description,
        href: product.href,
        ctaLabel: product.ctaLabel,
        meta: product.meta ?? undefined,
      })),
      emptyMessage: resolvedProductsMessage ?? copy.offerBoardEmptyMessage,
    },
    {
      id: "home-campaign-board",
      kind: "card-grid",
      eyebrow: copy.campaignBoardEyebrow,
      title: copy.campaignBoardTitle,
      description: copy.campaignBoardDescription,
      cards: categoryCampaignCards,
      emptyMessage:
        resolvedCategoriesMessage ??
        resolvedProductsMessage ??
        copy.campaignBoardEmptyMessage,
    },
    {
      id: "home-route-map",
      kind: "route-map",
      eyebrow: copy.routeMapEyebrow,
      title: copy.routeMapTitle,
      description: copy.routeMapDescription,
      items: [
        {
          id: "route-map-cms",
          label: copy.routeMapCmsLabel,
          title: copy.routeMapCmsTitle,
          description: cmsHealthy
            ? copy.routeMapCmsHealthyDescription
            : formatResource(copy.routeMapCmsDegradedDescription, {
                status: pagesResult.status,
              }),
          primaryHref: "/cms",
          primaryCtaLabel: copy.openCmsCta,
          secondaryHref: spotlightPage ? `/cms/${spotlightPage.slug}` : "/account",
          secondaryCtaLabel: spotlightPage
            ? copy.routeMapCmsSecondaryCta
            : copy.openAccountCta,
          meta: spotlightPage
            ? formatResource(copy.routeMapCmsMeta, {
                slug: spotlightPage.slug,
              })
            : copy.routeMapCmsFallbackMeta,
        },
        {
          id: "route-map-catalog",
          label: copy.routeMapCatalogLabel,
          title: copy.routeMapCatalogTitle,
          description:
            catalogHealthy && categoriesHealthy
              ? copy.routeMapCatalogHealthyDescription
              : formatResource(copy.routeMapCatalogDegradedDescription, {
                  productsStatus: productsResult.status,
                  categoriesStatus: categoriesResult.status,
                }),
          primaryHref: "/catalog",
          primaryCtaLabel: copy.browseCatalogCta,
          secondaryHref: spotlightProduct
            ? `/catalog/${spotlightProduct.slug}`
            : "/checkout",
          secondaryCtaLabel: spotlightProduct
            ? copy.routeMapCatalogSecondaryCta
            : copy.openCheckoutCta,
          meta: spotlightProduct
            ? formatResource(copy.routeMapCatalogMeta, {
                slug: spotlightProduct.slug,
              })
            : copy.routeMapCatalogFallbackMeta,
        },
        {
          id: "route-map-account",
          label: copy.routeMapAccountLabel,
          title: copy.routeMapAccountTitle,
          description: copy.routeMapAccountDescription,
          primaryHref: "/account",
          primaryCtaLabel: copy.openAccountCta,
          secondaryHref: "/loyalty",
          secondaryCtaLabel: copy.routeMapAccountSecondaryCta,
          meta: copy.routeMapAccountMeta,
        },
      ],
      emptyMessage: copy.routeMapEmptyMessage,
    },
    ...(session
      ? [
          {
            id: "home-member-resume",
            kind: "link-list",
            eyebrow: copy.memberResumeEyebrow,
            title: formatResource(copy.memberResumeTitle, {
              email: session.email,
            }),
            description: formatResource(copy.memberResumeDescription, {
              expiry: formatDateTime(session.accessTokenExpiresAtUtc, culture),
            }),
            items: [
              ...(recentOrders.length > 0
                ? recentOrders.map((order) => ({
                    id: `member-resume-order-${order.id}`,
                    title: formatResource(copy.memberResumeRecentOrderTitle, {
                      orderNumber: order.orderNumber,
                    }),
                    description: formatResource(
                      copy.memberResumeRecentOrderDescription,
                      {
                        createdAt: formatDateTime(order.createdAtUtc, culture),
                        total: formatMoney(
                          order.grandTotalGrossMinor,
                          order.currency,
                          culture,
                        ),
                      },
                    ),
                    href: `/orders/${order.id}`,
                    ctaLabel: copy.memberResumeRecentOrderCta,
                    meta: formatResource(copy.memberResumeRecentOrderMeta, {
                      status: memberCommerceContext?.ordersResult.status ?? "ok",
                    }),
                  }))
                : []),
              ...(invoiceAttention
                ? [
                    {
                      id: `member-resume-invoice-${invoiceAttention.id}`,
                      title: formatResource(copy.memberResumeInvoiceTitle, {
                        reference:
                          invoiceAttention.orderNumber ?? invoiceAttention.id,
                      }),
                      description: formatResource(
                        copy.memberResumeInvoiceDescription,
                        {
                          balance: formatMoney(
                            invoiceAttention.balanceMinor,
                            invoiceAttention.currency,
                            culture,
                          ),
                          status: invoiceAttention.status,
                        },
                      ),
                      href: `/invoices/${invoiceAttention.id}`,
                      ctaLabel: copy.memberResumeInvoiceCta,
                      meta: formatResource(copy.memberResumeInvoiceMeta, {
                        status: memberCommerceContext?.invoicesResult.status ?? "ok",
                      }),
                    },
                  ]
                : []),
              {
                id: "member-resume-checkout",
                title: preferredMemberAddress
                  ? copy.memberResumeCheckoutTitle
                  : copy.memberResumeCheckoutAddressTitle,
                description: preferredMemberAddress
                  ? formatResource(copy.memberResumeCheckoutDescription, {
                      city: preferredMemberAddress.city,
                      countryCode: preferredMemberAddress.countryCode,
                    })
                  : formatResource(copy.memberResumeCheckoutAddressDescription, {
                      status: memberIdentityContext?.addressesResult.status ?? "unauthenticated",
                    }),
                href: memberCheckoutHref,
                ctaLabel: preferredMemberAddress
                  ? copy.memberResumeCheckoutCta
                  : copy.memberResumeCheckoutAddressCta,
                meta: preferredMemberAddress
                  ? formatResource(copy.memberResumeCheckoutMeta, {
                      count: memberAddresses.length,
                    })
                  : formatResource(copy.memberResumeCheckoutAddressMeta, {
                      count: memberAddresses.length,
                    }),
              },
              ...(loyaltyFocusAccount
                ? [
                    {
                      id: `member-resume-loyalty-focus-${loyaltyFocusAccount.loyaltyAccountId}`,
                      title: formatResource(copy.memberResumeLoyaltyFocusTitle, {
                        business: loyaltyFocusAccount.businessName,
                      }),
                      description: formatResource(
                        copy.memberResumeLoyaltyFocusDescription,
                        {
                          points:
                            loyaltyFocusAccount.pointsToNextReward?.toString() ??
                            copy.memberResumeUnavailable,
                          reward:
                            loyaltyFocusAccount.nextRewardTitle ??
                            copy.memberResumeUnavailable,
                        },
                      ),
                      href: `/loyalty/${loyaltyFocusAccount.businessId}`,
                      ctaLabel: copy.memberResumeLoyaltyFocusCta,
                      meta: formatResource(copy.memberResumeLoyaltyFocusMeta, {
                        status: memberCommerceContext?.loyaltyOverviewResult.status ?? "ok",
                      }),
                    },
                  ]
                : []),
              {
                id: "member-resume-account",
                title: copy.memberResumeAccountTitle,
                description: copy.memberResumeAccountDescription,
                href: "/account",
                ctaLabel: copy.memberResumeAccountCta,
                meta: copy.memberResumeAccountMeta,
              },
              {
                id: "member-resume-orders",
                title: copy.memberResumeOrdersTitle,
                description: copy.memberResumeOrdersDescription,
                href: "/orders",
                ctaLabel: copy.memberResumeOrdersCta,
                meta: copy.memberResumeOrdersMeta,
              },
              {
                id: "member-resume-loyalty",
                title: copy.memberResumeLoyaltyTitle,
                description: copy.memberResumeLoyaltyDescription,
                href: "/loyalty",
                ctaLabel: copy.memberResumeLoyaltyCta,
                meta: copy.memberResumeLoyaltyMeta,
              },
            ],
      emptyMessage:
        resolvedOrdersMessage ??
        resolvedInvoicesMessage ??
        resolvedLoyaltyMessage ??
        copy.memberResumeEmptyMessage,
          } satisfies WebPagePart,
        ]
      : []),
    ...(cartSnapshots.length > 0
      ? [
          {
            id: "home-cart-resume",
            kind: "link-list",
            eyebrow: copy.cartResumeEyebrow,
            title: copy.cartResumeTitle,
            description: formatResource(copy.cartResumeDescription, {
              count: cartSnapshots.length,
            }),
            items: [
              ...cartSnapshots.map((snapshot) => ({
                id: `cart-resume-${snapshot.variantId}`,
                title: snapshot.name,
                description:
                  snapshot.sku && snapshot.sku.trim().length > 0
                    ? formatResource(copy.cartResumeItemDescription, {
                        sku: snapshot.sku,
                      })
                    : copy.cartResumeItemFallbackDescription,
                href: snapshot.href,
                ctaLabel: copy.cartResumeItemCta,
                meta: snapshot.imageAlt ?? copy.cartResumeItemMeta,
              })),
              {
                id: "cart-resume-cart",
                title: copy.cartResumeCartTitle,
                description: copy.cartResumeCartDescription,
                href: "/cart",
                ctaLabel: copy.cartResumeCartCta,
                meta: copy.cartResumeCartMeta,
              },
              {
                id: "cart-resume-checkout",
                title: copy.cartResumeCheckoutTitle,
                description: copy.cartResumeCheckoutDescription,
                href: "/checkout",
                ctaLabel: copy.cartResumeCheckoutCta,
                meta: copy.cartResumeCheckoutMeta,
              },
            ],
            emptyMessage: copy.cartResumeEmptyMessage,
          } satisfies WebPagePart,
        ]
      : []),
    ...(cartResult.data && cartResult.data.items.length > 0
      ? [
          {
            id: "home-cart-window",
            kind: "status-list",
            eyebrow: copy.cartWindowEyebrow,
            title: copy.cartWindowTitle,
            description: formatResource(copy.cartWindowDescription, {
              status: cartResult.status,
              itemCount: cartResult.data.items.length,
            }),
            items: [
              {
                id: "cart-window-cart",
                label: copy.cartWindowCartLabel,
                title: copy.cartWindowCartTitle,
                description: formatResource(copy.cartWindowCartDescription, {
                  itemCount: cartResult.data.items.length,
                }),
                href: "/cart",
                ctaLabel: copy.cartWindowCartCta,
                tone: "ok",
                meta: formatResource(copy.cartWindowCartMeta, {
                  total: formatMoney(
                    cartResult.data.grandTotalGrossMinor,
                    cartResult.data.currency,
                    culture,
                  ),
                }),
              },
              {
                id: "cart-window-checkout",
                label: copy.cartWindowCheckoutLabel,
                title: copy.cartWindowCheckoutTitle,
                description: cartResult.data.couponCode
                  ? formatResource(copy.cartWindowCheckoutCouponDescription, {
                      coupon: cartResult.data.couponCode,
                    })
                  : copy.cartWindowCheckoutDescription,
                href: "/checkout",
                ctaLabel: copy.cartWindowCheckoutCta,
                tone: "ok",
                meta: formatResource(copy.cartWindowCheckoutMeta, {
                  subtotal: formatMoney(
                    cartResult.data.subtotalNetMinor,
                    cartResult.data.currency,
                    culture,
                  ),
                }),
              },
            ],
            emptyMessage: copy.cartWindowEmptyMessage,
          } satisfies WebPagePart,
        ]
      : []),
    {
      id: "home-contract-rail",
      kind: "status-list",
      eyebrow: copy.contractRailEyebrow,
      title: copy.contractRailTitle,
      description: copy.contractRailDescription,
      items: [
        {
          id: "contract-cms",
          label: copy.contractCmsLabel,
          title: copy.contractCmsTitle,
          description: cmsHealthy
            ? copy.contractCmsHealthyDescription
            : formatResource(copy.contractCmsDegradedDescription, {
                status: pagesResult.status,
              }),
          href: "/cms",
          ctaLabel: copy.openCmsCta,
          tone: cmsHealthy ? "ok" : "warning",
          meta: formatResource(copy.contractCmsMeta, {
            count: pagesResult.data?.items.length ?? 0,
          }),
        },
        {
          id: "contract-catalog",
          label: copy.contractCatalogLabel,
          title: copy.contractCatalogTitle,
          description: catalogHealthy
            ? copy.contractCatalogHealthyDescription
            : formatResource(copy.contractCatalogDegradedDescription, {
                status: productsResult.status,
              }),
          href: "/catalog",
          ctaLabel: copy.browseCatalogCta,
          tone: catalogHealthy ? "ok" : "warning",
          meta: formatResource(copy.contractCatalogMeta, {
            count: productsResult.data?.items.length ?? 0,
          }),
        },
        {
          id: "contract-account",
          label: copy.contractAccountLabel,
          title: copy.contractAccountTitle,
          description: copy.contractAccountDescription,
          href: "/account",
          ctaLabel: copy.openAccountCta,
          tone: "ok",
          meta: formatResource(copy.contractAccountMeta, {
            count: supportedCultures.length,
          }),
        },
      ],
      emptyMessage: copy.contractRailEmptyMessage,
    },
    {
      id: "home-stage-flow",
      kind: "stage-flow",
      eyebrow: copy.stageFlowEyebrow,
      title: copy.stageFlowTitle,
      description: copy.stageFlowDescription,
      items: [
        {
          id: "stage-content",
          step: copy.stageContentStep,
          title: copy.stageContentTitle,
          description: copy.stageContentDescription,
          href: "/cms",
          ctaLabel: copy.openCmsCta,
          meta: formatResource(copy.stageContentMeta, {
            status: pagesResult.status,
          }),
        },
        {
          id: "stage-discovery",
          step: copy.stageDiscoveryStep,
          title: copy.stageDiscoveryTitle,
          description: copy.stageDiscoveryDescription,
          href: "/catalog",
          ctaLabel: copy.browseCatalogCta,
          meta: formatResource(copy.stageDiscoveryMeta, {
            status: productsResult.status,
          }),
        },
        {
          id: "stage-follow-up",
          step: copy.stageFollowUpStep,
          title: copy.stageFollowUpTitle,
          description: copy.stageFollowUpDescription,
          href: "/account",
          ctaLabel: copy.openAccountCta,
          meta: copy.stageFollowUpMeta,
        },
      ],
      emptyMessage: copy.stageFlowEmptyMessage,
    },
    {
      id: "home-pair-panel",
      kind: "pair-panel",
      eyebrow: copy.pairPanelEyebrow,
      title: copy.pairPanelTitle,
      description: copy.pairPanelDescription,
      leading: {
        id: "pair-cms",
        eyebrow: copy.pairCmsEyebrow,
        title: copy.pairCmsTitle,
        description: cmsHealthy
          ? copy.pairCmsHealthyDescription
          : formatResource(copy.pairCmsDegradedDescription, {
              status: pagesResult.status,
            }),
        href: "/cms",
        ctaLabel: copy.openCmsCta,
        meta: formatResource(copy.pairCmsMeta, {
          count: pagesResult.data?.items.length ?? 0,
        }),
      },
      trailing: {
        id: "pair-catalog",
        eyebrow: copy.pairCatalogEyebrow,
        title: copy.pairCatalogTitle,
        description: catalogHealthy
          ? copy.pairCatalogHealthyDescription
          : formatResource(copy.pairCatalogDegradedDescription, {
              status: productsResult.status,
            }),
        href: "/catalog",
        ctaLabel: copy.browseCatalogCta,
        meta: formatResource(copy.pairCatalogMeta, {
          count: productsResult.data?.items.length ?? 0,
        }),
      },
    },
    {
      id: "home-agenda-columns",
      kind: "agenda-columns",
      eyebrow: copy.agendaEyebrow,
      title: copy.agendaTitle,
      description: copy.agendaDescription,
      columns: [
        {
          id: "agenda-content",
          label: copy.agendaContentLabel,
          title: copy.agendaContentTitle,
          description: copy.agendaContentDescription,
          href: "/cms",
          ctaLabel: copy.openCmsCta,
          bullets: [
            copy.agendaContentBulletOne,
            formatResource(copy.agendaContentBulletTwo, {
              status: pagesResult.status,
            }),
          ],
          meta: formatResource(copy.agendaContentMeta, {
            count: pagesResult.data?.items.length ?? 0,
          }),
        },
        {
          id: "agenda-commerce",
          label: copy.agendaCommerceLabel,
          title: copy.agendaCommerceTitle,
          description: copy.agendaCommerceDescription,
          href: "/catalog",
          ctaLabel: copy.browseCatalogCta,
          bullets: [
            copy.agendaCommerceBulletOne,
            formatResource(copy.agendaCommerceBulletTwo, {
              status: productsResult.status,
            }),
          ],
          meta: formatResource(copy.agendaCommerceMeta, {
            count: productsResult.data?.items.length ?? 0,
          }),
        },
        {
          id: "agenda-member",
          label: copy.agendaMemberLabel,
          title: copy.agendaMemberTitle,
          description: copy.agendaMemberDescription,
          href: "/account",
          ctaLabel: copy.openAccountCta,
          bullets: [
            copy.agendaMemberBulletOne,
            copy.agendaMemberBulletTwo,
          ],
          meta: formatResource(copy.agendaMemberMeta, {
            count: supportedCultures.length,
          }),
        },
      ],
      emptyMessage: copy.agendaEmptyMessage,
    },
    {
      id: "home-category-spotlight",
      kind: "card-grid",
      eyebrow: copy.categorySpotlightEyebrow,
      title: copy.categorySpotlightTitle,
      description: copy.categorySpotlightDescription,
      cards: categorySpotlights.map(({ category, product, status }) => ({
        id: category.id,
        eyebrow: copy.categoryEyebrow,
        title: category.name,
        description:
          product && status === "ok"
            ? formatResource(copy.categorySpotlightCardDescription, {
                productName: product.name,
              })
            : category.description ??
              formatResource(copy.categorySpotlightFallbackDescription, {
                status,
              }),
        href: buildAppQueryPath("/catalog", {
          category: category.slug,
        }),
        ctaLabel: copy.categorySpotlightCta,
        meta:
          product && status === "ok"
            ? formatResource(copy.categorySpotlightMeta, {
                price: formatMoney(product.priceMinor, product.currency, culture),
              })
            : formatResource(copy.categorySpotlightFallbackMeta, {
                status,
              }),
      })),
      emptyMessage:
        resolvedCategoriesMessage ?? copy.categorySpotlightEmptyMessage,
    },
    {
      id: "home-shortcuts",
      kind: "card-grid",
      eyebrow: copy.shortcutsEyebrow,
      title: copy.shortcutsTitle,
      description: copy.shortcutsDescription,
      cards: [
        {
          id: "shortcut-cms",
          eyebrow: copy.shortcutCmsEyebrow,
          title: copy.shortcutCmsTitle,
          description: copy.shortcutCmsDescription,
          href: "/cms",
          ctaLabel: copy.openCmsCta,
        },
        {
          id: "shortcut-catalog",
          eyebrow: copy.shortcutCatalogEyebrow,
          title: copy.shortcutCatalogTitle,
          description: copy.shortcutCatalogDescription,
          href: "/catalog",
          ctaLabel: copy.browseCatalogCta,
        },
        {
          id: "shortcut-account",
          eyebrow: copy.shortcutAccountEyebrow,
          title: copy.shortcutAccountTitle,
          description: copy.shortcutAccountDescription,
          href: "/account",
          ctaLabel: copy.openAccountCta,
        },
      ],
      emptyMessage: copy.shortcutsEmptyMessage,
    },
    {
      id: "home-journeys",
      kind: "link-list",
      eyebrow: copy.journeysEyebrow,
      title: copy.journeysTitle,
      description: copy.journeysDescription,
      items: [
        {
          id: "journey-cms",
          title: copy.journeyCmsTitle,
          description: copy.journeyCmsDescription,
          href: "/cms",
          ctaLabel: copy.readPageCta,
          meta: formatResource(copy.journeyCmsMeta, {
            status: pagesResult.status,
          }),
        },
        {
          id: "journey-catalog",
          title: copy.journeyCatalogTitle,
          description: copy.journeyCatalogDescription,
          href: "/catalog",
          ctaLabel: copy.browseCatalogCta,
          meta: formatResource(copy.journeyCatalogMeta, {
            status: productsResult.status,
          }),
        },
        {
          id: "journey-account",
          title: copy.journeyAccountTitle,
          description: copy.journeyAccountDescription,
          href: "/account",
          ctaLabel: copy.openAccountCta,
          meta: copy.journeyAccountMeta,
        },
      ],
      emptyMessage: copy.journeysEmptyMessage,
    },
    {
      id: "home-recovery-rail",
      kind: "link-list",
      eyebrow: copy.recoveryRailEyebrow,
      title: copy.recoveryRailTitle,
      description: copy.recoveryRailDescription,
      items: [
        {
          id: "recovery-cms",
          title: copy.recoveryCmsTitle,
          description: cmsHealthy
            ? copy.recoveryCmsHealthyDescription
            : formatResource(copy.recoveryCmsDegradedDescription, {
                status: pagesResult.status,
              }),
          href: "/cms",
          ctaLabel: copy.openCmsCta,
          meta: formatResource(copy.recoveryCmsMeta, {
            count: pagesResult.data?.items.length ?? 0,
          }),
        },
        {
          id: "recovery-catalog",
          title: copy.recoveryCatalogTitle,
          description: catalogHealthy && categoriesHealthy
            ? copy.recoveryCatalogHealthyDescription
            : formatResource(copy.recoveryCatalogDegradedDescription, {
                productsStatus: productsResult.status,
                categoriesStatus: categoriesResult.status,
              }),
          href: "/catalog",
          ctaLabel: copy.browseCatalogCta,
          meta: formatResource(copy.recoveryCatalogMeta, {
            count: productsResult.data?.items.length ?? 0,
          }),
        },
        {
          id: "recovery-account",
          title: copy.recoveryAccountTitle,
          description: copy.recoveryAccountDescription,
          href: "/account",
          ctaLabel: copy.openAccountCta,
          meta: copy.recoveryAccountMeta,
        },
      ],
      emptyMessage: copy.recoveryRailEmptyMessage,
    },
    {
      id: "home-cms-spotlight",
      kind: "card-grid",
      eyebrow: copy.cmsSpotlightEyebrow,
      title: copy.cmsSpotlightTitle,
      description: copy.cmsSpotlightDescription,
      cards:
        pagesResult.data?.items.map((page) => ({
          id: page.id,
          eyebrow: copy.cmsPageEyebrow,
          title: page.title,
          description: page.metaDescription ?? copy.cmsPageDescriptionFallback,
          href: `/cms/${page.slug}`,
          ctaLabel: copy.readPageCta,
          meta: page.slug,
        })) ?? [],
      emptyMessage: resolvedPagesMessage ?? copy.cmsSpotlightEmptyMessage,
    },
    {
      id: "home-product-spotlight",
      kind: "card-grid",
      eyebrow: copy.productSpotlightEyebrow,
      title: copy.productSpotlightTitle,
      description: copy.productSpotlightDescription,
      cards:
        rankedProducts.map((product) => ({
          id: product.id,
          eyebrow: copy.productEyebrow,
          title: product.name,
          description: product.shortDescription ?? copy.productDescriptionFallback,
          href: `/catalog/${product.slug}`,
          ctaLabel: copy.viewProductCta,
          meta: formatMoney(product.priceMinor, product.currency, culture),
        })) ?? [],
      emptyMessage:
        resolvedProductsMessage ?? copy.productSpotlightEmptyMessage,
    },
  ];
}
