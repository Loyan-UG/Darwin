import Link from "next/link";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import {
  getProductSavingsPercent,
  sortProductsByOpportunity,
} from "@/features/catalog/merchandising";
import { formatMoney } from "@/lib/formatting";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
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
    ...categories.slice(0, 2).map((category) => ({
      id: `cms-campaign-category-${category.id}`,
      label: copy.cmsCampaignCategoryLabel,
      title: formatResource(copy.cmsCampaignCategoryTitle, {
        category: category.name,
      }),
      description:
        category.description ?? copy.cmsCampaignCategoryFallbackDescription,
      href: localizeHref(
        buildAppQueryPath("/catalog", {
          category: category.slug,
        }),
        culture,
      ),
      ctaLabel: copy.cmsCampaignCategoryCta,
      meta: formatResource(copy.cmsCampaignCategoryMeta, {
        status: categoriesStatus,
      }),
    })),
    ...rankedProducts.slice(0, 2).map((product) => {
      const savingsPercent = getProductSavingsPercent(product);

      return {
        id: `cms-campaign-product-${product.id}`,
        label: copy.cmsCampaignProductLabel,
        title: formatResource(copy.cmsCampaignProductTitle, {
          product: product.name,
        }),
        description:
          savingsPercent !== null
            ? formatResource(copy.cmsCampaignProductDescription, {
                savingsPercent,
                price: formatMoney(product.priceMinor, product.currency, culture),
              })
            : formatResource(copy.cmsCampaignProductFallbackDescription, {
                price: formatMoney(product.priceMinor, product.currency, culture),
              }),
        href: localizeHref(`/catalog/${product.slug}`, culture),
        ctaLabel: copy.cmsCampaignProductCta,
        meta: formatResource(copy.cmsCampaignProductMeta, {
          status: productsStatus,
        }),
      };
    }),
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
      {campaignCards.length > 0 ? (
        <div className="mt-5 grid gap-3 md:grid-cols-2">
          {campaignCards.map((card) => (
            <Link
              key={card.id}
              href={card.href}
              className="rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 transition hover:bg-[var(--color-surface-panel)]"
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
              <div className="mt-4 flex items-center justify-between gap-3">
                <span className="text-sm font-semibold text-[var(--color-brand)]">
                  {card.ctaLabel}
                </span>
                <span className="text-xs font-medium text-[var(--color-text-muted)]">
                  {card.meta}
                </span>
              </div>
            </Link>
          ))}
        </div>
      ) : (
        <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
          {formatResource(copy.cmsCampaignWindowEmptyMessage, {
            categoriesStatus,
            productsStatus,
          })}
        </p>
      )}
    </section>
  );
}
