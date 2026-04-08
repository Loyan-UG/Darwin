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
import { formatResource, getCatalogResource } from "@/localization";

type CatalogCampaignWindowProps = {
  culture: string;
  categories: PublicCategorySummary[];
  products: PublicProductSummary[];
};

export function CatalogCampaignWindow({
  culture,
  categories,
  products,
}: CatalogCampaignWindowProps) {
  const copy = getCatalogResource(culture);
  const rankedProducts = sortProductsByOpportunity(products);
  const cards = [
    ...buildStorefrontCategoryCampaignCards(categories.slice(0, 2), {
      prefix: "catalog-campaign",
      label: copy.campaignWindowCategoryLabel,
      fallbackDescription: copy.campaignWindowCategoryFallbackDescription,
      ctaLabel: copy.campaignWindowCategoryCta,
    }).map((card) => ({
      ...card,
      title: formatResource(copy.campaignWindowCategoryTitle, {
        category: card.title,
      }),
    })),
    ...buildStorefrontProductCampaignCards(rankedProducts.slice(0, 2), {
      prefix: "catalog-campaign",
      labels: {
        heroOffer: copy.campaignWindowProductHeroLabel,
        valueOffer: copy.campaignWindowProductValueLabel,
        priceDrop: copy.campaignWindowProductPriceDropLabel,
        steadyPick: copy.campaignWindowProductSteadyLabel,
      },
      formatPrice: (product) =>
        formatMoney(product.priceMinor, product.currency, culture),
      describeWithSavings: (product, input) =>
        formatResource(copy.campaignWindowProductDescription, {
          product: product.name,
          campaignLabel: input.campaignLabel,
          savingsPercent: input.savingsPercent,
          price: input.price,
        }),
      describeWithoutSavings: (product, input) =>
        formatResource(copy.campaignWindowProductFallbackDescription, {
          product: product.name,
          campaignLabel: input.campaignLabel,
          price: input.price,
        }),
      ctaLabel: copy.campaignWindowProductCta,
    }).map((card) => ({
      ...card,
      title: formatResource(copy.campaignWindowProductTitle, {
        product: card.title,
      }),
    })),
  ];

  return (
    <section className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
        {copy.campaignWindowTitle}
      </p>
      <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
        {formatResource(copy.campaignWindowMessage, {
          categoryCount: categories.length,
          productCount: products.length,
        })}
      </p>
      <StorefrontCampaignBoard
        culture={culture}
        cards={cards}
        emptyMessage={copy.campaignWindowEmptyMessage}
        columnsClassName="md:grid-cols-2"
        cardClassName="bg-[var(--color-surface-panel-strong)]"
      />
    </section>
  );
}
