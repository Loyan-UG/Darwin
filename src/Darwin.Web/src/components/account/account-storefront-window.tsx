import Link from "next/link";
import { StorefrontCampaignBoard } from "@/components/storefront/storefront-campaign-board";
import { StorefrontOfferBoard } from "@/components/storefront/storefront-offer-board";
import { StorefrontSpotlightBoard } from "@/components/storefront/storefront-spotlight-board";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import {
  buildStorefrontCategoryCampaignCards,
  buildStorefrontCategorySpotlightLinkCards,
  buildStorefrontOfferCards,
  buildStorefrontPageSpotlightCards,
  buildStorefrontProductCampaignCards,
} from "@/features/storefront/storefront-campaigns";
import { buildStorefrontSpotlightSelections } from "@/features/storefront/storefront-spotlight";
import { localizeHref } from "@/lib/locale-routing";
import { formatMoney } from "@/lib/formatting";
import { formatResource, getMemberResource } from "@/localization";

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
    <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
      <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
        {copy.accountStorefrontWindowTitle}
      </p>
      <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
        {formatResource(copy.accountStorefrontWindowMessage, {
          cmsStatus: cmsPagesStatus,
          categoriesStatus,
          productsStatus,
          pageCount: cmsPages.length,
          categoryCount: categories.length,
          productCount: products.length,
        })}
      </p>
      <div className="mt-6 grid gap-4">
        <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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
              status: cmsPagesStatus,
            })}
          />
        </div>

        <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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
              status: categoriesStatus,
            })}
          />
        </div>

        <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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
              status: productsStatus,
            })}
            columnsClassName="grid-cols-1"
          />
        </div>

        <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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
          <StorefrontCampaignBoard
            culture={culture}
            cards={campaignBoard}
            emptyMessage={copy.accountStorefrontCampaignEmptyMessage}
          />
        </div>
      </div>
    </aside>
  );
}
