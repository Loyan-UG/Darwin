import Link from "next/link";
import {
  PublicContinuationRail,
  type PublicContinuationItem,
} from "@/components/shell/public-continuation-rail";
import { StorefrontCampaignBoard } from "@/components/storefront/storefront-campaign-board";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicCartSummary } from "@/features/cart/types";
import type { PublicPageSummary } from "@/features/cms/types";
import { summarizeCatalogPromotionLanes } from "@/features/catalog/promotion-lanes";
import {
  buildStorefrontCategoryCampaignCards,
  buildStorefrontCategorySpotlightLinkCards,
  buildStorefrontOfferCards,
  buildStorefrontPageSpotlightCards,
  buildStorefrontProductCampaignCards,
} from "@/features/storefront/storefront-campaigns";
import { buildStorefrontSpotlightSelections } from "@/features/storefront/storefront-spotlight";
import { formatMoney } from "@/lib/formatting";
import { localizeHref } from "@/lib/locale-routing";
import {
  formatResource,
  getMemberResource,
  resolveApiStatusLabel,
} from "@/localization";

type PublicAuthContinuationProps = {
  culture: string;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
  storefrontCart: PublicCartSummary | null;
  storefrontCartStatus: string;
};

export function PublicAuthContinuation({
  culture,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
  storefrontCart,
  storefrontCartStatus,
}: PublicAuthContinuationProps) {
  const copy = getMemberResource(culture);
  const cmsPagesStatusLabel = resolveApiStatusLabel(cmsPagesStatus, copy) ?? cmsPagesStatus;
  const categoriesStatusLabel =
    resolveApiStatusLabel(categoriesStatus, copy) ?? categoriesStatus;
  const productsStatusLabel = resolveApiStatusLabel(productsStatus, copy) ?? productsStatus;
  const storefrontCartStatusLabel =
    resolveApiStatusLabel(storefrontCartStatus, copy) ?? storefrontCartStatus;
  const cartLineCount =
    storefrontCart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0;
  const {
    campaignCategories,
    campaignProducts,
    rankedProducts: productOpportunities,
  } = buildStorefrontSpotlightSelections({
    cmsPages,
    categories,
    products,
    categoryCampaignCount: 2,
    productCampaignCount: 2,
  });
  const campaignLabels = {
    heroOffer: copy.offerCampaignHeroLabel,
    valueOffer: copy.offerCampaignValueLabel,
    priceDrop: copy.offerCampaignPriceDropLabel,
    steadyPick: copy.offerCampaignSteadyLabel,
  };
  const promotionLaneCards = summarizeCatalogPromotionLanes(productOpportunities).map(
    (entry) => {
      const laneLabel =
        entry.lane === "hero-offers"
          ? copy.publicAuthPromotionLaneHeroLabel
          : entry.lane === "value-offers"
            ? copy.publicAuthPromotionLaneValueLabel
            : entry.lane === "live-offers"
              ? copy.publicAuthPromotionLaneLiveOffersLabel
              : copy.publicAuthPromotionLaneBaseLabel;
      const href =
        entry.lane === "hero-offers"
          ? "/catalog?visibleState=offers&visibleSort=offers-first&savingsBand=hero"
          : entry.lane === "value-offers"
            ? "/catalog?visibleState=offers&visibleSort=offers-first&savingsBand=value"
            : entry.lane === "live-offers"
              ? "/catalog?visibleState=offers&visibleSort=savings-desc"
              : "/catalog?visibleState=base&visibleSort=base-first";

      return {
        id: `auth-promotion-lane-${entry.lane}`,
        label: copy.publicAuthPromotionLaneCardLabel,
        title: entry.anchorProduct
          ? formatResource(copy.publicAuthPromotionLaneTitle, {
              lane: laneLabel,
              product: entry.anchorProduct.name,
            })
          : formatResource(copy.publicAuthPromotionLaneFallbackTitle, {
              lane: laneLabel,
            }),
        description:
          entry.anchorProduct !== null
            ? formatResource(copy.publicAuthPromotionLaneDescription, {
                lane: laneLabel,
                count: entry.count,
                price: formatMoney(
                  entry.anchorProduct.priceMinor,
                  entry.anchorProduct.currency,
                  culture,
                ),
              })
            : formatResource(copy.publicAuthPromotionLaneFallbackDescription, {
                lane: laneLabel,
              }),
        href,
        ctaLabel: copy.publicAuthPromotionLaneCta,
        meta: formatResource(copy.publicAuthPromotionLaneMeta, {
          count: entry.count,
        }),
      };
    },
  );
  const campaignCards = [
    ...buildStorefrontCategoryCampaignCards(campaignCategories, {
      prefix: "auth-campaign",
      label: copy.publicAuthCampaignCategoryLabel,
      fallbackDescription: copy.publicAuthCampaignCategoryFallbackDescription,
      ctaLabel: copy.publicAuthCampaignCategoryCta,
    }),
    ...buildStorefrontProductCampaignCards(campaignProducts, {
      prefix: "auth-campaign",
      labels: campaignLabels,
      formatPrice: (product) =>
        formatMoney(product.priceMinor, product.currency, culture),
      describeWithSavings: (_, input) =>
        formatResource(copy.publicAuthCampaignProductDescription, {
          campaignLabel: input.campaignLabel,
          savingsPercent: input.savingsPercent,
          price: input.price,
        }),
      describeWithoutSavings: (_, input) =>
        formatResource(copy.publicAuthCampaignProductFallbackDescription, {
          campaignLabel: input.campaignLabel,
          price: input.price,
        }),
      ctaLabel: copy.publicAuthCampaignProductCta,
    }),
  ];
  const productOpportunityCards = buildStorefrontOfferCards(
    productOpportunities,
    {
      labels: campaignLabels,
      formatPrice: (product) =>
        formatMoney(product.priceMinor, product.currency, culture),
      describeWithSavings: (_, input) =>
        formatResource(copy.publicAuthProductOfferDescription, {
          campaignLabel: input.campaignLabel,
          savingsPercent: input.savingsPercent,
          price: input.price,
        }),
      describeWithoutSavings: (product) =>
        product.shortDescription ?? copy.publicAuthProductFallbackDescription,
      fallbackDescription: copy.publicAuthProductFallbackDescription,
      ctaLabel: copy.publicAuthProductCta,
    },
  );
  const cmsSpotlightCards = buildStorefrontPageSpotlightCards(cmsPages, {
    prefix: "auth",
    fallbackDescription: copy.publicAuthCmsFallbackDescription,
  });
  const categorySpotlightCards = buildStorefrontCategorySpotlightLinkCards(categories, {
    prefix: "auth",
    fallbackDescription: copy.publicAuthCatalogFallbackDescription,
  });

  const items: PublicContinuationItem[] = [
    ...(storefrontCart && cartLineCount > 0
      ? [
          {
            id: "auth-cart",
            label: copy.publicAuthCartLabel,
            title: copy.publicAuthCartTitle,
            description: formatResource(copy.publicAuthCartDescription, {
              itemCount: cartLineCount,
              total: formatMoney(
                storefrontCart.grandTotalGrossMinor,
                storefrontCart.currency,
                culture,
              ),
              status: storefrontCartStatusLabel,
            }),
            href: "/cart",
            ctaLabel: copy.publicAuthCartCta,
          },
        ]
      : [
          {
            id: "auth-cart",
            label: copy.publicAuthCartLabel,
            title: copy.publicAuthCartTitle,
            description:
              storefrontCartStatus === "ok"
                ? copy.publicAuthCartFallbackDescription
                : formatResource(copy.publicAuthCartEmptyMessage, {
                    status: storefrontCartStatusLabel,
                  }),
            href: "/cart",
            ctaLabel: copy.publicAuthCartCta,
          },
        ]),
    {
      id: "auth-home",
      label: copy.memberCrossSurfaceTitle,
      title: copy.accountHubHomeTitle,
      description: copy.accountHubHomeDescription,
      href: "/",
      ctaLabel: copy.memberCrossSurfaceHomeCta,
    },
    ...(cmsSpotlightCards.length > 0
      ? cmsSpotlightCards.map((page) => ({
          id: page.id,
          label: copy.accountHubCmsLabel,
          title: page.title,
          description: page.description,
          href: page.href,
          ctaLabel: copy.accountHubCmsCta,
        }))
      : [
          {
            id: "auth-cms",
            label: copy.accountHubCmsLabel,
            title: copy.accountHubCmsTitle,
            description:
              cmsPagesStatus === "ok"
                ? copy.publicAuthCmsFallbackDescription
                : formatResource(copy.publicAuthCmsEmptyMessage, {
                    status: cmsPagesStatusLabel,
                  }),
            href: "/cms",
            ctaLabel: copy.accountHubCmsCta,
          },
        ]),
    ...(categorySpotlightCards.length > 0
      ? categorySpotlightCards.map((category) => ({
          id: category.id,
          label: copy.accountHubCatalogLabel,
          title: category.title,
          description: category.description,
          href: category.href,
          ctaLabel: copy.memberCrossSurfaceCatalogCta,
        }))
      : [
          {
            id: "auth-catalog",
            label: copy.accountHubCatalogLabel,
            title: copy.accountHubCatalogTitle,
            description:
              categoriesStatus === "ok"
                ? copy.publicAuthCatalogFallbackDescription
                : formatResource(copy.publicAuthCatalogEmptyMessage, {
                    status: categoriesStatusLabel,
                  }),
            href: "/catalog",
            ctaLabel: copy.memberCrossSurfaceCatalogCta,
          },
        ]),
    ...(productOpportunityCards.length > 0
      ? productOpportunityCards.map((product) => {
          return {
            id: `auth-product-${product.id}`,
            label: product.label,
            title: product.title,
            description: product.description,
            href: product.href,
            ctaLabel: product.ctaLabel ?? copy.publicAuthProductCta,
          };
        })
      : [
          {
            id: "auth-product",
            label: copy.publicAuthProductLabel,
            title: copy.publicAuthProductTitle,
            description:
              productsStatus === "ok"
                ? copy.publicAuthProductFallbackDescription
                : formatResource(copy.publicAuthProductEmptyMessage, {
                    status: productsStatusLabel,
                  }),
            href: "/catalog",
            ctaLabel: copy.publicAuthProductCta,
          },
        ]),
  ];

  return (
    <div className="flex flex-col gap-6">
      <PublicContinuationRail
        culture={culture}
        eyebrow={copy.memberCrossSurfaceTitle}
        title={copy.accountHubRouteMapTitle}
        description={formatResource(copy.publicAuthStorefrontWindowMessage, {
          cartStatus: storefrontCartStatusLabel,
          cartLineCount,
          cmsStatus: cmsPagesStatusLabel,
          categoriesStatus: categoriesStatusLabel,
          productsStatus: productsStatusLabel,
          pageCount: cmsPages.length,
          categoryCount: categories.length,
          productCount: products.length,
        })}
        items={items}
      />
      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <div className="flex items-center justify-between gap-3">
          <p className="text-sm font-semibold text-[var(--color-text-primary)]">
            {copy.publicAuthCampaignTitle}
          </p>
          <Link
            href={localizeHref("/catalog", culture)}
            className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
          >
            {copy.publicAuthCampaignCta}
          </Link>
        </div>
        <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
          {formatResource(copy.publicAuthCampaignMessage, {
            categoryCount: categories.length,
            productCount: products.length,
            categoriesStatus: categoriesStatusLabel,
            productsStatus: productsStatusLabel,
          })}
        </p>
        <div className="mt-4 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
            {copy.publicAuthPromotionLaneSectionTitle}
          </p>
          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
            {copy.publicAuthPromotionLaneSectionMessage}
          </p>
          <StorefrontCampaignBoard
            culture={culture}
            cards={promotionLaneCards}
            emptyMessage={formatResource(copy.publicAuthCampaignEmptyMessage, {
              categoriesStatus: categoriesStatusLabel,
              productsStatus: productsStatusLabel,
            })}
            cardClassName="bg-[var(--color-surface-panel)]"
          />
        </div>
        <StorefrontCampaignBoard
          culture={culture}
          cards={campaignCards}
          emptyMessage={formatResource(copy.publicAuthCampaignEmptyMessage, {
            categoriesStatus: categoriesStatusLabel,
            productsStatus: productsStatusLabel,
          })}
          cardClassName="bg-[var(--color-surface-panel-strong)]"
        />
      </section>
    </div>
  );
}
