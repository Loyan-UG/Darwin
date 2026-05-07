import Link from "next/link";
import { StorefrontCampaignBoard } from "@/components/storefront/storefront-campaign-board";
import { StorefrontOfferBoard } from "@/components/storefront/storefront-offer-board";
import { StorefrontSpotlightBoard } from "@/components/storefront/storefront-spotlight-board";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
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
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import { formatMoney } from "@/lib/formatting";
import {
  formatResource,
  getMemberResource,
  resolveApiStatusLabel,
} from "@/localization";

type AccountStorefrontWindowProps = {
  culture: string;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
};

export function AccountStorefrontWindow({
  culture,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
}: AccountStorefrontWindowProps) {
  const copy = getMemberResource(culture);
  const localizedCmsPagesStatus =
    resolveApiStatusLabel(cmsPagesStatus, copy) ?? cmsPagesStatus;
  const localizedCategoriesStatus =
    resolveApiStatusLabel(categoriesStatus, copy) ?? categoriesStatus;
  const localizedProductsStatus =
    resolveApiStatusLabel(productsStatus, copy) ?? productsStatus;
  const {
    offerBoardProducts: productOpportunities,
    campaignCategories,
    campaignProducts,
  } = buildStorefrontSpotlightSelections({
    cmsPages,
    categories,
    products,
    categoryCampaignCount: 2,
    productCampaignCount: 2,
    offerBoardCount: 3,
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
          ? copy.accountStorefrontPromotionLaneHeroLabel
          : entry.lane === "value-offers"
            ? copy.accountStorefrontPromotionLaneValueLabel
            : entry.lane === "live-offers"
              ? copy.accountStorefrontPromotionLaneLiveOffersLabel
              : copy.accountStorefrontPromotionLaneBaseLabel;
      const href =
        entry.lane === "hero-offers"
          ? buildAppQueryPath("/catalog", {
              visibleState: "offers",
              visibleSort: "offers-first",
              savingsBand: "hero",
            })
          : entry.lane === "value-offers"
            ? buildAppQueryPath("/catalog", {
                visibleState: "offers",
                visibleSort: "offers-first",
                savingsBand: "value",
              })
            : entry.lane === "live-offers"
              ? buildAppQueryPath("/catalog", {
                  visibleState: "offers",
                  visibleSort: "savings-desc",
                })
              : buildAppQueryPath("/catalog", {
                  visibleState: "base",
                  visibleSort: "base-first",
                });

      return {
        id: `account-promotion-lane-${entry.lane}`,
        label: copy.accountStorefrontPromotionLaneCardLabel,
        title: entry.anchorProduct
          ? formatResource(copy.accountStorefrontPromotionLaneTitle, {
              lane: laneLabel,
              product: entry.anchorProduct.name,
            })
          : formatResource(copy.accountStorefrontPromotionLaneFallbackTitle, {
              lane: laneLabel,
            }),
        description:
          entry.anchorProduct !== null
            ? formatResource(copy.accountStorefrontPromotionLaneDescription, {
                lane: laneLabel,
                count: entry.count,
                price: formatMoney(
                  entry.anchorProduct.priceMinor,
                  entry.anchorProduct.currency,
                  culture,
                ),
              })
            : formatResource(copy.accountStorefrontPromotionLaneFallbackDescription, {
                lane: laneLabel,
              }),
        href,
        ctaLabel: copy.accountStorefrontPromotionLaneCta,
        meta: formatResource(copy.accountStorefrontPromotionLaneMeta, {
          count: entry.count,
        }),
      };
    },
  );
  const campaignBoard = [
    ...buildStorefrontCategoryCampaignCards(campaignCategories, {
      prefix: "account-campaign",
      label: copy.accountStorefrontCampaignCategoryLabel,
      fallbackDescription:
        copy.accountStorefrontCampaignCategoryFallbackDescription,
      ctaLabel: copy.accountStorefrontCampaignCategoryCta,
    }),
    ...buildStorefrontProductCampaignCards(campaignProducts, {
      prefix: "account-campaign",
      labels: campaignLabels,
      formatPrice: (product) =>
        formatMoney(product.priceMinor, product.currency, culture),
      describeWithSavings: (_, input) =>
        formatResource(copy.accountStorefrontCampaignProductDescription, {
          campaignLabel: input.campaignLabel,
          savingsPercent: input.savingsPercent,
          price: input.price,
        }),
      describeWithoutSavings: (_, input) =>
        formatResource(copy.accountStorefrontCampaignProductFallbackDescription, {
          campaignLabel: input.campaignLabel,
          price: input.price,
        }),
      ctaLabel: copy.accountStorefrontCampaignProductCta,
    }),
  ];
  const productOpportunityCards = buildStorefrontOfferCards(
    productOpportunities,
    {
      labels: campaignLabels,
      formatPrice: (product) =>
        formatMoney(product.priceMinor, product.currency, culture),
      describeWithSavings: (_, input) =>
        formatResource(copy.accountStorefrontProductOfferDescription, {
          campaignLabel: input.campaignLabel,
          savingsPercent: input.savingsPercent,
          price: input.price,
        }),
      describeWithoutSavings: (product) =>
        product.shortDescription ?? copy.accountStorefrontProductFallbackDescription,
      fallbackDescription: copy.accountStorefrontProductFallbackDescription,
      formatMeta: (product) =>
        typeof product.compareAtPriceMinor === "number"
          ? formatResource(copy.accountStorefrontProductOfferMeta, {
              compareAt: formatMoney(
                product.compareAtPriceMinor,
                product.currency,
                culture,
              ),
            })
          : null,
    },
  );
  const cmsSpotlightCards = buildStorefrontPageSpotlightCards(cmsPages, {
    prefix: "account",
    fallbackDescription: copy.accountStorefrontCmsFallbackDescription,
  });
  const categorySpotlightCards = buildStorefrontCategorySpotlightLinkCards(categories, {
    prefix: "account",
    fallbackDescription: copy.accountStorefrontCatalogFallbackDescription,
  });

  return (
    <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
      <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
        {copy.accountStorefrontWindowTitle}
      </p>
      <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
        {formatResource(copy.accountStorefrontWindowMessage, {
          cmsStatus: localizedCmsPagesStatus,
          categoriesStatus: localizedCategoriesStatus,
          productsStatus: localizedProductsStatus,
          pageCount: cmsPages.length,
          categoryCount: categories.length,
          productCount: products.length,
        })}
      </p>
      <div className="mt-6 grid gap-4">
        <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
          <div className="flex items-center justify-between gap-3">
            <p className="text-sm font-semibold text-[var(--color-text-primary)]">
              {copy.accountStorefrontCmsTitle}
            </p>
            <Link
              href={localizeHref("/cms", culture)}
              className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
            >
              {copy.accountStorefrontCmsCta}
            </Link>
          </div>
          <StorefrontSpotlightBoard
            culture={culture}
            cards={cmsSpotlightCards}
            emptyMessage={formatResource(copy.accountStorefrontCmsEmptyMessage, {
              status: localizedCmsPagesStatus,
            })}
          />
        </div>

        <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
          <div className="flex items-center justify-between gap-3">
            <p className="text-sm font-semibold text-[var(--color-text-primary)]">
              {copy.accountStorefrontCatalogTitle}
            </p>
            <Link
              href={localizeHref("/catalog", culture)}
              className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
            >
              {copy.accountStorefrontCatalogCta}
            </Link>
          </div>
          <StorefrontSpotlightBoard
            culture={culture}
            cards={categorySpotlightCards}
            emptyMessage={formatResource(copy.accountStorefrontCatalogEmptyMessage, {
              status: localizedCategoriesStatus,
            })}
          />
        </div>

        <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
          <div className="flex items-center justify-between gap-3">
            <p className="text-sm font-semibold text-[var(--color-text-primary)]">
              {copy.accountStorefrontProductTitle}
            </p>
            <Link
              href={localizeHref("/catalog", culture)}
              className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
            >
              {copy.accountStorefrontProductCta}
            </Link>
          </div>
          <StorefrontOfferBoard
            culture={culture}
            cards={productOpportunityCards}
            emptyMessage={formatResource(copy.accountStorefrontProductEmptyMessage, {
              status: localizedProductsStatus,
            })}
            columnsClassName="grid-cols-1"
          />
        </div>

        <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
          <div className="flex items-center justify-between gap-3">
            <p className="text-sm font-semibold text-[var(--color-text-primary)]">
              {copy.accountStorefrontCampaignTitle}
            </p>
            <Link
              href={localizeHref("/catalog", culture)}
              className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
            >
              {copy.accountStorefrontCampaignCta}
            </Link>
          </div>
          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.accountStorefrontCampaignMessage, {
              categoryCount: categories.length,
              productCount: products.length,
            })}
          </p>
          <div className="mt-4 rounded-[1rem] bg-[var(--color-surface-panel)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
              {copy.accountStorefrontPromotionLaneSectionTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.accountStorefrontPromotionLaneSectionMessage}
            </p>
            <StorefrontCampaignBoard
              culture={culture}
              cards={promotionLaneCards}
              emptyMessage={copy.accountStorefrontCampaignEmptyMessage}
              cardClassName="bg-[var(--color-surface-panel-strong)]"
            />
          </div>
          <StorefrontCampaignBoard
            culture={culture}
            cards={campaignBoard}
            emptyMessage={copy.accountStorefrontCampaignEmptyMessage}
          />
        </div>

        <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
          <p className="text-sm font-semibold text-[var(--color-text-primary)]">
            {copy.accountStorefrontCartTitle}
          </p>
          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
            {copy.accountStorefrontCartMessage}
          </p>
          <div className="mt-4 flex flex-wrap gap-3">
            <Link
              href={localizeHref("/cart", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
            >
              {copy.accountStorefrontCartCartCta}
            </Link>
            <Link
              href={localizeHref("/checkout", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
            >
              {copy.accountStorefrontCartCheckoutCta}
            </Link>
          </div>
        </div>
      </div>
    </aside>
  );
}

