import Link from "next/link";
import { StorefrontOfferBoard } from "@/components/storefront/storefront-offer-board";
import { StorefrontSpotlightBoard } from "@/components/storefront/storefront-spotlight-board";
import { sortProductsByOpportunity } from "@/features/catalog/merchandising";
import type { PublicProductSummary } from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import {
  buildStorefrontOfferCards,
  buildStorefrontPageSpotlightCards,
} from "@/features/storefront/storefront-campaigns";
import { formatMoney } from "@/lib/formatting";
import { localizeHref } from "@/lib/locale-routing";
import { formatResource, getCatalogResource } from "@/localization";

type CatalogStorefrontSupportWindowProps = {
  culture: string;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
  cartSummary: {
    status: string;
    itemCount: number;
    currency: string;
    grandTotalGrossMinor: number;
  } | null;
};

export function CatalogStorefrontSupportWindow({
  culture,
  cmsPages,
  cmsPagesStatus,
  products,
  productsStatus,
  cartSummary,
}: CatalogStorefrontSupportWindowProps) {
  const copy = getCatalogResource(culture);
  const cmsCards = buildStorefrontPageSpotlightCards(cmsPages, {
    prefix: "catalog-support-cms",
    fallbackDescription: copy.catalogCmsWindowFallbackDescription,
  });
  const productCards = buildStorefrontOfferCards(sortProductsByOpportunity(products), {
    labels: {
      heroOffer: copy.campaignWindowProductHeroLabel,
      valueOffer: copy.campaignWindowProductValueLabel,
      priceDrop: copy.campaignWindowProductPriceDropLabel,
      steadyPick: copy.campaignWindowProductSteadyLabel,
    },
    formatPrice: (product) => formatMoney(product.priceMinor, product.currency, culture),
    describeWithSavings: (_, input) =>
      formatResource(copy.catalogProductsWindowOfferDescription, {
        savingsPercent: input.savingsPercent,
        price: input.price,
      }),
    describeWithoutSavings: (product) =>
      product.shortDescription ?? copy.catalogProductsWindowFallbackDescription,
    fallbackDescription: copy.catalogProductsWindowFallbackDescription,
    ctaLabel: copy.catalogProductsWindowCta,
  });

  return (
    <div
      id="catalog-storefront-support"
      className="scroll-mt-28 grid gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]"
    >
      <section className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.catalogCmsWindowTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.catalogCmsWindowMessage, {
                cmsPagesStatus,
                pageCount: cmsPages.length,
              })}
            </p>
          </div>
          <Link
            href={localizeHref("/cms", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.catalogCmsWindowCta}
          </Link>
        </div>
        <StorefrontSpotlightBoard
          culture={culture}
          cards={cmsCards}
          emptyMessage={formatResource(copy.catalogCmsWindowEmptyMessage, {
            status: cmsPagesStatus,
          })}
        />
      </section>

      <section className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.catalogProductsWindowTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.catalogProductsWindowMessage, {
                productsStatus,
                productCount: products.length,
              })}
            </p>
          </div>
          <Link
            href={localizeHref("/catalog", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.catalogProductsWindowCta}
          </Link>
        </div>
        <StorefrontOfferBoard
          culture={culture}
          cards={productCards}
          emptyMessage={formatResource(copy.catalogProductsWindowEmptyMessage, {
            status: productsStatus,
          })}
        />
      </section>

      <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)] lg:col-span-2">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
          {copy.catalogCartWindowTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {cartSummary
            ? formatResource(copy.catalogCartWindowMessage, {
                itemCount: cartSummary.itemCount,
                total: formatMoney(
                  cartSummary.grandTotalGrossMinor,
                  cartSummary.currency,
                  culture,
                ),
              })
            : copy.catalogCartWindowFallback}
        </p>
        <div className="mt-5 flex flex-wrap gap-3">
          <Link
            href={localizeHref("/cart", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.catalogCartWindowCartCta}
          </Link>
          <Link
            href={localizeHref("/checkout", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.catalogCartWindowCheckoutCta}
          </Link>
        </div>
      </div>
    </div>
  );
}
