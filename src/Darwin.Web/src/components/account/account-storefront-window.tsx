import Link from "next/link";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import {
  getProductOpportunityCampaign,
  getProductOpportunityCampaignLabel,
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
  const campaignBoard = [
    ...categories.slice(0, 2).map((category) => ({
      id: `account-campaign-category-${category.id}`,
      label: copy.accountStorefrontCampaignCategoryLabel,
      title: category.name,
      description:
        category.description ?? copy.accountStorefrontCampaignCategoryFallbackDescription,
      href: buildAppQueryPath("/catalog", { category: category.slug }),
      ctaLabel: copy.accountStorefrontCampaignCategoryCta,
    })),
    ...productOpportunities.slice(0, 2).map((product) => {
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

      return {
        id: `account-campaign-product-${product.id}`,
        label: campaignLabel,
        title: product.name,
        description:
          savingsPercent !== null
            ? formatResource(copy.accountStorefrontCampaignProductDescription, {
                campaignLabel,
                savingsPercent,
                price: formatMoney(product.priceMinor, product.currency, culture),
              })
            : formatResource(copy.accountStorefrontCampaignProductFallbackDescription, {
                campaignLabel,
                price: formatMoney(product.priceMinor, product.currency, culture),
              }),
        href: `/catalog/${product.slug}`,
        ctaLabel: copy.accountStorefrontCampaignProductCta,
      };
    }),
  ];

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
                const campaignLabel = getProductOpportunityCampaignLabel(
                  getProductOpportunityCampaign(product),
                  {
                    heroOffer: copy.offerCampaignHeroLabel,
                    valueOffer: copy.offerCampaignValueLabel,
                    priceDrop: copy.offerCampaignPriceDropLabel,
                    steadyPick: copy.offerCampaignSteadyLabel,
                  },
                );
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
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {campaignLabel}
                    </p>
                    <p className="font-semibold text-[var(--color-text-primary)]">
                      {product.name}
                    </p>
                    <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {savingsPercent !== null
                        ? formatResource(copy.accountStorefrontProductOfferDescription, {
                            campaignLabel,
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
              {copy.accountStorefrontCampaignEmptyMessage}
            </p>
          )}
        </div>
      </div>
    </aside>
  );
}
