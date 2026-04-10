import Link from "next/link";
import { StorefrontOfferBoard } from "@/components/storefront/storefront-offer-board";
import { StorefrontSpotlightBoard } from "@/components/storefront/storefront-spotlight-board";
import { sortProductsByOpportunity } from "@/features/catalog/merchandising";
import type { PublicCategorySummary, PublicProductSummary } from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import {
  buildStorefrontCategorySpotlightLinkCards,
  buildStorefrontOfferCards,
  buildStorefrontPageSpotlightCards,
} from "@/features/storefront/storefront-campaigns";
import { formatMoney } from "@/lib/formatting";
import { localizeHref } from "@/lib/locale-routing";
import { formatResource, getCatalogResource } from "@/localization";

type ProductStorefrontSupportWindowProps = {
  culture: string;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
  cartSummary: {
    status: string;
    itemCount: number;
    currency: string;
    grandTotalGrossMinor: number;
  } | null;
};

export function ProductStorefrontSupportWindow({
  culture,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
  cartSummary,
}: ProductStorefrontSupportWindowProps) {
  const copy = getCatalogResource(culture);
  const rankedProducts = sortProductsByOpportunity(products);
  const cmsCards = buildStorefrontPageSpotlightCards(cmsPages, {
    prefix: "product-support-cms",
    fallbackDescription: copy.productCmsWindowFallbackDescription,
  });
  const categoryCards = buildStorefrontCategorySpotlightLinkCards(categories, {
    prefix: "product-support-category",
    fallbackDescription: copy.productCatalogWindowFallbackDescription,
  });
  const productCards = buildStorefrontOfferCards(rankedProducts, {
    labels: {
      heroOffer: copy.campaignWindowProductHeroLabel,
      valueOffer: copy.campaignWindowProductValueLabel,
      priceDrop: copy.campaignWindowProductPriceDropLabel,
      steadyPick: copy.campaignWindowProductSteadyLabel,
    },
    formatPrice: (product) => formatMoney(product.priceMinor, product.currency, culture),
    describeWithSavings: (_, input) =>
      formatResource(copy.productProductsWindowOfferDescription, {
        savingsPercent: input.savingsPercent,
        price: input.price,
      }),
    describeWithoutSavings: (product) =>
      product.shortDescription ?? copy.productProductsWindowFallbackDescription,
    fallbackDescription: copy.productProductsWindowFallbackDescription,
    ctaLabel: copy.productProductsWindowCta,
  });

  return (
    <div
      id="product-storefront-support"
      className="scroll-mt-28 grid gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]"
    >
      <section className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.productCmsWindowTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.productCmsWindowMessage, {
                cmsPagesStatus,
                pageCount: cmsPages.length,
              })}
            </p>
          </div>
          <Link
            href={localizeHref("/cms", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.productCmsWindowCta}
          </Link>
        </div>
        <StorefrontSpotlightBoard
          culture={culture}
          cards={cmsCards}
          emptyMessage={formatResource(copy.productCmsWindowEmptyMessage, {
            status: cmsPagesStatus,
          })}
        />
      </section>

      <section className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.productCatalogWindowTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.productCatalogWindowMessage, {
                categoriesStatus,
                categoryCount: categories.length,
              })}
            </p>
          </div>
          <Link
            href={localizeHref("/catalog", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.productCatalogWindowCta}
          </Link>
        </div>
        <StorefrontSpotlightBoard
          culture={culture}
          cards={categoryCards}
          emptyMessage={formatResource(copy.productCatalogWindowEmptyMessage, {
            status: categoriesStatus,
          })}
        />
      </section>

      <section className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)] lg:col-span-2">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.productProductsWindowTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.productProductsWindowMessage, {
                productsStatus,
                productCount: products.length,
              })}
            </p>
          </div>
          <Link
            href={localizeHref("/catalog", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.productProductsWindowCta}
          </Link>
        </div>
        <StorefrontOfferBoard
          culture={culture}
          cards={productCards}
          emptyMessage={formatResource(copy.productProductsWindowEmptyMessage, {
            status: productsStatus,
          })}
        />
      </section>

      <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)] lg:col-span-2">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
          {copy.productCartWindowTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {cartSummary
            ? formatResource(copy.productCartWindowMessage, {
                itemCount: cartSummary.itemCount,
                total: formatMoney(
                  cartSummary.grandTotalGrossMinor,
                  cartSummary.currency,
                  culture,
                ),
              })
            : copy.productCartWindowFallback}
        </p>
        <div className="mt-5 flex flex-wrap gap-3">
          <Link
            href={localizeHref("/cart", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.productCartWindowCartCta}
          </Link>
          <Link
            href={localizeHref("/checkout", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.productCartWindowCheckoutCta}
          </Link>
        </div>
      </div>
    </div>
  );
}
