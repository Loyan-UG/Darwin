import Link from "next/link";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import {
  getProductOpportunityCampaign,
  getProductSavingsPercent,
  sortProductsByOpportunity,
} from "@/features/catalog/merchandising";
import { formatMoney } from "@/lib/formatting";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
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
    ...categories.slice(0, 2).map((category) => ({
      id: `catalog-campaign-category-${category.id}`,
      label: copy.campaignWindowCategoryLabel,
      title: formatResource(copy.campaignWindowCategoryTitle, {
        category: category.name,
      }),
      description:
        category.description ?? copy.campaignWindowCategoryFallbackDescription,
      href: localizeHref(
        buildAppQueryPath("/catalog", { category: category.slug }),
        culture,
      ),
      ctaLabel: copy.campaignWindowCategoryCta,
    })),
    ...rankedProducts.slice(0, 2).map((product) => {
      const savingsPercent = getProductSavingsPercent(product);
      const campaign = getProductOpportunityCampaign(product);

      const campaignLabel =
        campaign === "hero-offer"
          ? copy.campaignWindowProductHeroLabel
          : campaign === "value-offer"
            ? copy.campaignWindowProductValueLabel
            : campaign === "price-drop"
              ? copy.campaignWindowProductPriceDropLabel
              : copy.campaignWindowProductSteadyLabel;

      return {
        id: `catalog-campaign-product-${product.id}`,
        label: campaignLabel,
        title: formatResource(copy.campaignWindowProductTitle, {
          product: product.name,
        }),
        description:
          savingsPercent !== null
            ? formatResource(copy.campaignWindowProductDescription, {
                campaignLabel,
                savingsPercent,
                price: formatMoney(product.priceMinor, product.currency, culture),
              })
            : formatResource(copy.campaignWindowProductFallbackDescription, {
                campaignLabel,
                price: formatMoney(product.priceMinor, product.currency, culture),
              }),
        href: localizeHref(`/catalog/${product.slug}`, culture),
        ctaLabel: copy.campaignWindowProductCta,
      };
    }),
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
      {cards.length > 0 ? (
        <div className="mt-5 grid gap-4 md:grid-cols-2">
          {cards.map((card) => (
            <Link
              key={card.id}
              href={card.href}
              className="rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-5 py-5 transition hover:bg-[var(--color-surface-panel)]"
            >
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {card.label}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {card.title}
              </p>
              <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                {card.description}
              </p>
              <p className="mt-4 text-sm font-semibold text-[var(--color-brand)]">
                {card.ctaLabel}
              </p>
            </Link>
          ))}
        </div>
      ) : (
        <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.campaignWindowEmptyMessage}
        </p>
      )}
    </section>
  );
}
