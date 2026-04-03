import Link from "next/link";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import {
  getProductOpportunityCampaign,
  getProductOpportunityCampaignLabel,
  getProductSavingsPercent,
  sortProductsByOpportunity,
} from "@/features/catalog/merchandising";
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
  const offerBoard = sortProductsByOpportunity(products).slice(0, 3);
  const campaignBoard = [
    ...categories.slice(0, 2).map((category) => ({
      id: `commerce-campaign-category-${category.id}`,
      label: copy.storefrontWindowCampaignCategoryLabel,
      title: category.name,
      description:
        category.description ?? copy.storefrontWindowCampaignCategoryFallbackDescription,
      href: buildAppQueryPath("/catalog", { category: category.slug }),
      ctaLabel: copy.storefrontWindowCampaignCategoryCta,
    })),
    ...offerBoard.slice(0, 2).map((product) => {
      const savingsPercent = getProductSavingsPercent(product);

      return {
        id: `commerce-campaign-product-${product.id}`,
        label: copy.storefrontWindowCampaignProductLabel,
        title: product.name,
        description:
          savingsPercent !== null
            ? formatResource(copy.storefrontWindowCampaignProductDescription, {
                savingsPercent,
                price: formatMoney(product.priceMinor, product.currency, culture),
              })
            : formatResource(copy.storefrontWindowCampaignProductFallbackDescription, {
                price: formatMoney(product.priceMinor, product.currency, culture),
              }),
        href: `/catalog/${product.slug}`,
        ctaLabel: copy.storefrontWindowCampaignProductCta,
      };
    }),
  ];

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
          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.storefrontWindowProductOfferBoardMessage, {
              productCount: products.length,
              status: productsStatus,
            })}
          </p>
          <div className="mt-4 flex flex-wrap gap-3">
            <Link
              href={localizeHref("/catalog", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
            >
              {copy.storefrontWindowProductOfferBoardCta}
            </Link>
          </div>
          {offerBoard.length > 0 ? (
            <div className="mt-4 flex flex-col gap-3">
              {offerBoard.map((product) => {
                const savingsPercent = getProductSavingsPercent(product);
                const campaignLabel = getProductOpportunityCampaignLabel(
                  getProductOpportunityCampaign(product),
                  {
                    heroOffer: copy.offerCampaignHeroLabel,
                    valueOffer: copy.offerCampaignValueLabel,
                    priceDrop: copy.offerCampaignPriceDropLabel,
                    steadyPick: copy.offerCampaignSteadyLabel,
                  },
                );

                return (
                  <Link
                    key={product.id}
                    href={localizeHref(`/catalog/${product.slug}`, culture)}
                    className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0">
                        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                          {campaignLabel}
                        </p>
                        <p className="truncate font-semibold text-[var(--color-text-primary)]">
                          {product.name}
                        </p>
                        <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                          {savingsPercent !== null
                            ? formatResource(
                                copy.storefrontWindowProductOfferBoardDescription,
                                {
                                  campaignLabel,
                                  savingsPercent,
                                  price: formatMoney(
                                    product.priceMinor,
                                    product.currency,
                                    culture,
                                  ),
                                },
                              )
                            : product.shortDescription ??
                              copy.storefrontWindowProductOfferBoardFallbackDescription}
                        </p>
                        {typeof product.compareAtPriceMinor === "number" ? (
                          <p className="mt-2 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                            {formatResource(copy.storefrontWindowProductOfferBoardMeta, {
                              compareAt: formatMoney(
                                product.compareAtPriceMinor,
                                product.currency,
                                culture,
                              ),
                            })}
                          </p>
                        ) : null}
                      </div>
                      <div className="shrink-0 text-right">
                        <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                          {formatMoney(product.priceMinor, product.currency, culture)}
                        </p>
                        <p className="mt-2 text-sm font-semibold text-[var(--color-brand)]">
                          {copy.storefrontWindowProductSpotlightCta}
                        </p>
                      </div>
                    </div>
                  </Link>
                );
              })}
            </div>
          ) : (
            <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.storefrontWindowProductOfferBoardEmptyMessage, {
                status: productsStatus,
              })}
            </p>
          )}
        </article>
      </div>

      <div className="mt-5 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
        <div className="flex items-center justify-between gap-3">
          <p className="text-sm font-semibold text-[var(--color-text-primary)]">
            {copy.storefrontWindowCampaignTitle}
          </p>
          <Link
            href={localizeHref("/catalog", culture)}
            className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
          >
            {copy.storefrontWindowCampaignCta}
          </Link>
        </div>
        <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
          {formatResource(copy.storefrontWindowCampaignMessage, {
            categoryCount: categories.length,
            productCount: products.length,
          })}
        </p>
        {campaignBoard.length > 0 ? (
          <div className="mt-4 grid gap-3 lg:grid-cols-2">
            {campaignBoard.map((item) => (
              <Link
                key={item.id}
                href={localizeHref(item.href, culture)}
                className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {item.label}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {item.title}
                </p>
                <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {item.description}
                </p>
                <p className="mt-3 text-sm font-semibold text-[var(--color-brand)]">
                  {item.ctaLabel}
                </p>
              </Link>
            ))}
          </div>
        ) : (
          <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
            {copy.storefrontWindowCampaignEmptyMessage}
          </p>
        )}
      </div>
    </section>
  );
}
