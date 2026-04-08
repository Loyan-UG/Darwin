import Link from "next/link";
import { StorefrontCampaignBoard } from "@/components/storefront/storefront-campaign-board";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import { sortProductsByOpportunity } from "@/features/catalog/merchandising";
import {
  buildStorefrontCategoryCampaignCards,
  buildStorefrontProductCampaignCards,
} from "@/features/storefront/storefront-campaigns";
import { formatMoney } from "@/lib/formatting";
import { localizeHref } from "@/lib/locale-routing";
import { formatResource, getSharedResource } from "@/localization";

type CmsCommerceCampaignWindowProps = {
  culture: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
};

export function CmsCommerceCampaignWindow({
  culture,
  categories,
  categoriesStatus,
  products,
  productsStatus,
}: CmsCommerceCampaignWindowProps) {
  const copy = getSharedResource(culture);
  const rankedProducts = sortProductsByOpportunity(products);
  const campaignCards = [
    ...buildStorefrontCategoryCampaignCards(categories.slice(0, 2), {
      prefix: "cms-campaign",
      label: copy.cmsCampaignCategoryLabel,
      fallbackDescription: copy.cmsCampaignCategoryFallbackDescription,
      ctaLabel: copy.cmsCampaignCategoryCta,
    }).map((card) => ({
      ...card,
      title: formatResource(copy.cmsCampaignCategoryTitle, {
        category: card.title,
      }),
      meta: formatResource(copy.cmsCampaignCategoryMeta, {
        status: categoriesStatus,
      }),
    })),
    ...buildStorefrontProductCampaignCards(rankedProducts.slice(0, 2), {
      prefix: "cms-campaign",
      labels: {
        heroOffer: copy.cmsCampaignProductHeroLabel,
        valueOffer: copy.cmsCampaignProductValueLabel,
        priceDrop: copy.cmsCampaignProductPriceDropLabel,
        steadyPick: copy.cmsCampaignProductSteadyLabel,
      },
      formatPrice: (product) =>
        formatMoney(product.priceMinor, product.currency, culture),
      describeWithSavings: (_product, input) =>
        formatResource(copy.cmsCampaignProductDescription, {
          campaignLabel: input.campaignLabel,
          savingsPercent: input.savingsPercent,
          price: input.price,
        }),
      describeWithoutSavings: (_product, input) =>
        formatResource(copy.cmsCampaignProductFallbackDescription, {
          campaignLabel: input.campaignLabel,
          price: input.price,
        }),
      ctaLabel: copy.cmsCampaignProductCta,
    }).map((card) => ({
      ...card,
      title: formatResource(copy.cmsCampaignProductTitle, {
        product: card.title,
      }),
      meta: formatResource(copy.cmsCampaignProductMeta, {
        status: productsStatus,
      }),
    })),
  ];

  return (
    <section className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
            {copy.cmsCampaignWindowTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.cmsCampaignWindowMessage, {
              categoryCount: categories.length,
              productCount: products.length,
            })}
          </p>
        </div>
        <Link
          href={localizeHref("/catalog", culture)}
          className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
        >
          {copy.cmsCampaignWindowCta}
        </Link>
      </div>
      <StorefrontCampaignBoard
        culture={culture}
        cards={campaignCards}
        emptyMessage={formatResource(copy.cmsCampaignWindowEmptyMessage, {
          categoriesStatus,
          productsStatus,
        })}
        columnsClassName="md:grid-cols-2"
        cardClassName="bg-[var(--color-surface-panel-strong)]"
      />
    </section>
  );
}
