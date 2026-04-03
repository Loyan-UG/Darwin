import Link from "next/link";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import {
  getProductSavingsPercent,
  sortProductsByOpportunity,
} from "@/features/catalog/merchandising";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
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
  const productOpportunities = sortProductsByOpportunity(products).slice(0, 3);

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
          {cmsPages.length > 0 ? (
            <div className="mt-4 flex flex-col gap-3">
              {cmsPages.map((page) => (
                <Link
                  key={page.id}
                  href={localizeHref(`/cms/${page.slug}`, culture)}
                  className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  <p className="font-semibold text-[var(--color-text-primary)]">
                    {page.title}
                  </p>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {page.metaDescription ?? copy.accountStorefrontCmsFallbackDescription}
                  </p>
                </Link>
              ))}
            </div>
          ) : (
            <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.accountStorefrontCmsEmptyMessage, {
                status: cmsPagesStatus,
              })}
            </p>
          )}
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
          {categories.length > 0 ? (
            <div className="mt-4 flex flex-col gap-3">
              {categories.map((category) => (
                <Link
                  key={category.id}
                  href={localizeHref(
                    buildAppQueryPath("/catalog", {
                      category: category.slug,
                    }),
                    culture,
                  )}
                  className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  <p className="font-semibold text-[var(--color-text-primary)]">
                    {category.name}
                  </p>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {category.description ?? copy.accountStorefrontCatalogFallbackDescription}
                  </p>
                </Link>
              ))}
            </div>
          ) : (
            <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.accountStorefrontCatalogEmptyMessage, {
                status: categoriesStatus,
              })}
            </p>
          )}
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
          {productOpportunities.length > 0 ? (
            <div className="mt-4 flex flex-col gap-3">
              {productOpportunities.map((product) => {
                const savingsPercent = getProductSavingsPercent(product);
                const compareAtPrice =
                  typeof product.compareAtPriceMinor === "number"
                    ? formatMoney(
                        product.compareAtPriceMinor,
                        product.currency,
                        culture,
                      )
                    : null;

                return (
                  <Link
                    key={product.id}
                    href={localizeHref(`/catalog/${product.slug}`, culture)}
                    className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    <p className="font-semibold text-[var(--color-text-primary)]">
                      {product.name}
                    </p>
                    <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {savingsPercent !== null
                        ? formatResource(copy.accountStorefrontProductOfferDescription, {
                            savingsPercent,
                            price: formatMoney(
                              product.priceMinor,
                              product.currency,
                              culture,
                            ),
                          })
                        : product.shortDescription ??
                          copy.accountStorefrontProductFallbackDescription}
                    </p>
                    {compareAtPrice ? (
                      <p className="mt-2 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                        {formatResource(copy.accountStorefrontProductOfferMeta, {
                          compareAt: compareAtPrice,
                        })}
                      </p>
                    ) : null}
                    <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                      {formatMoney(product.priceMinor, product.currency, culture)}
                    </p>
                  </Link>
                );
              })}
            </div>
          ) : (
            <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.accountStorefrontProductEmptyMessage, {
                status: productsStatus,
              })}
            </p>
          )}
        </div>
      </div>
    </aside>
  );
}
