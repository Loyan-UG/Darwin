import Link from "next/link";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import { formatMoney } from "@/lib/formatting";
import { formatResource, getCommerceResource } from "@/localization";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";

type CommerceStorefrontWindowProps = {
  culture: string;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
  title?: string;
  description?: string;
};

export function CommerceStorefrontWindow({
  culture,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
  title,
  description,
}: CommerceStorefrontWindowProps) {
  const copy = getCommerceResource(culture);
  const spotlightPage = cmsPages[0] ?? null;
  const spotlightCategory = categories[0] ?? null;
  const spotlightProduct = products[0] ?? null;

  return (
    <section className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
        {title ?? copy.storefrontWindowTitle}
      </p>
      <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
        {description ??
          formatResource(copy.storefrontWindowMessage, {
            cmsPagesStatus,
            categoriesStatus,
            productsStatus,
            cmsCount: cmsPages.length,
            categoryCount: categories.length,
            productCount: products.length,
          })}
      </p>

      <div className="mt-5 grid gap-4 xl:grid-cols-3">
        <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
            {copy.storefrontWindowCmsLabel}
          </p>
          <p className="mt-3 text-lg font-semibold text-[var(--color-text-primary)]">
            {spotlightPage?.title ?? copy.storefrontWindowCmsFallbackTitle}
          </p>
          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
            {spotlightPage?.metaDescription ?? copy.storefrontWindowCmsFallbackMessage}
          </p>
          <div className="mt-4 flex flex-wrap gap-3">
            <Link
              href={localizeHref("/cms", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
            >
              {copy.storefrontWindowCmsIndexCta}
            </Link>
            {spotlightPage ? (
              <Link
                href={localizeHref(`/cms/${spotlightPage.slug}`, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.storefrontWindowCmsSpotlightCta}
              </Link>
            ) : null}
          </div>
        </article>

        <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
            {copy.storefrontWindowCatalogLabel}
          </p>
          <p className="mt-3 text-lg font-semibold text-[var(--color-text-primary)]">
            {spotlightCategory?.name ?? copy.storefrontWindowCatalogFallbackTitle}
          </p>
          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
            {spotlightCategory?.description ?? copy.storefrontWindowCatalogFallbackMessage}
          </p>
          <div className="mt-4 flex flex-wrap gap-3">
            <Link
              href={localizeHref("/catalog", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
            >
              {copy.storefrontWindowCatalogIndexCta}
            </Link>
            {spotlightCategory ? (
              <Link
                href={localizeHref(
                  buildAppQueryPath("/catalog", {
                    category: spotlightCategory.slug,
                  }),
                  culture,
                )}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.storefrontWindowCatalogSpotlightCta}
              </Link>
            ) : null}
          </div>
        </article>

        <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
            {copy.storefrontWindowProductLabel}
          </p>
          <p className="mt-3 text-lg font-semibold text-[var(--color-text-primary)]">
            {spotlightProduct?.name ?? copy.storefrontWindowProductFallbackTitle}
          </p>
          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
            {spotlightProduct?.shortDescription ?? copy.storefrontWindowProductFallbackMessage}
          </p>
          {spotlightProduct ? (
            <p className="mt-4 text-sm font-semibold text-[var(--color-text-primary)]">
              {formatMoney(spotlightProduct.priceMinor, spotlightProduct.currency, culture)}
            </p>
          ) : null}
          <div className="mt-4 flex flex-wrap gap-3">
            <Link
              href={localizeHref("/catalog", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
            >
              {copy.storefrontWindowProductIndexCta}
            </Link>
            {spotlightProduct ? (
              <Link
                href={localizeHref(`/catalog/${spotlightProduct.slug}`, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.storefrontWindowProductSpotlightCta}
              </Link>
            ) : null}
          </div>
        </article>
      </div>
    </section>
  );
}
