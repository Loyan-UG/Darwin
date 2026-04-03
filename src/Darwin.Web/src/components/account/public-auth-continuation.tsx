import Link from "next/link";
import {
  PublicContinuationRail,
  type PublicContinuationItem,
} from "@/components/shell/public-continuation-rail";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicCartSummary } from "@/features/cart/types";
import type { PublicPageSummary } from "@/features/cms/types";
import {
  getProductOpportunityCampaign,
  getProductOpportunityCampaignLabel,
  getProductSavingsPercent,
  sortProductsByOpportunity,
} from "@/features/catalog/merchandising";
import { formatMoney } from "@/lib/formatting";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import { formatResource, getMemberResource } from "@/localization";

type PublicAuthContinuationProps = {
  culture: string;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
  storefrontCart: PublicCartSummary | null;
  storefrontCartStatus: string;
};

export function PublicAuthContinuation({
  culture,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
  storefrontCart,
  storefrontCartStatus,
}: PublicAuthContinuationProps) {
  const copy = getMemberResource(culture);
  const cartLineCount =
    storefrontCart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0;
  const productOpportunities = sortProductsByOpportunity(products);
  const campaignCards = [
    ...categories.slice(0, 2).map((category) => ({
      id: `auth-campaign-category-${category.id}`,
      label: copy.publicAuthCampaignCategoryLabel,
      title: category.name,
      description:
        category.description ?? copy.publicAuthCampaignCategoryFallbackDescription,
      href: buildAppQueryPath("/catalog", { category: category.slug }),
      ctaLabel: copy.publicAuthCampaignCategoryCta,
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
        id: `auth-campaign-product-${product.id}`,
        label: campaignLabel,
        title: product.name,
        description:
          savingsPercent !== null
            ? formatResource(copy.publicAuthCampaignProductDescription, {
                campaignLabel,
                savingsPercent,
                price: formatMoney(product.priceMinor, product.currency, culture),
              })
            : formatResource(copy.publicAuthCampaignProductFallbackDescription, {
                campaignLabel,
                price: formatMoney(product.priceMinor, product.currency, culture),
              }),
        href: `/catalog/${product.slug}`,
        ctaLabel: copy.publicAuthCampaignProductCta,
      };
    }),
  ];

  const items: PublicContinuationItem[] = [
    ...(storefrontCart && cartLineCount > 0
      ? [
          {
            id: "auth-cart",
            label: copy.publicAuthCartLabel,
            title: copy.publicAuthCartTitle,
            description: formatResource(copy.publicAuthCartDescription, {
              itemCount: cartLineCount,
              total: formatMoney(
                storefrontCart.grandTotalGrossMinor,
                storefrontCart.currency,
                culture,
              ),
              status: storefrontCartStatus,
            }),
            href: "/cart",
            ctaLabel: copy.publicAuthCartCta,
          },
        ]
      : [
          {
            id: "auth-cart",
            label: copy.publicAuthCartLabel,
            title: copy.publicAuthCartTitle,
            description:
              storefrontCartStatus === "ok"
                ? copy.publicAuthCartFallbackDescription
                : formatResource(copy.publicAuthCartEmptyMessage, {
                    status: storefrontCartStatus,
                  }),
            href: "/cart",
            ctaLabel: copy.publicAuthCartCta,
          },
        ]),
    {
      id: "auth-home",
      label: copy.memberCrossSurfaceTitle,
      title: copy.accountHubHomeTitle,
      description: copy.accountHubHomeDescription,
      href: "/",
      ctaLabel: copy.memberCrossSurfaceHomeCta,
    },
    ...(cmsPages.length > 0
      ? cmsPages.map((page) => ({
          id: `auth-cms-${page.id}`,
          label: copy.accountHubCmsLabel,
          title: page.title,
          description:
            page.metaDescription ?? copy.publicAuthCmsFallbackDescription,
          href: `/cms/${page.slug}`,
          ctaLabel: copy.accountHubCmsCta,
        }))
      : [
          {
            id: "auth-cms",
            label: copy.accountHubCmsLabel,
            title: copy.accountHubCmsTitle,
            description:
              cmsPagesStatus === "ok"
                ? copy.publicAuthCmsFallbackDescription
                : formatResource(copy.publicAuthCmsEmptyMessage, {
                    status: cmsPagesStatus,
                  }),
            href: "/cms",
            ctaLabel: copy.accountHubCmsCta,
          },
        ]),
    ...(categories.length > 0
      ? categories.map((category) => ({
          id: `auth-catalog-${category.id}`,
          label: copy.accountHubCatalogLabel,
          title: category.name,
          description:
            category.description ?? copy.publicAuthCatalogFallbackDescription,
          href: buildAppQueryPath("/catalog", { category: category.slug }),
          ctaLabel: copy.memberCrossSurfaceCatalogCta,
        }))
      : [
          {
            id: "auth-catalog",
            label: copy.accountHubCatalogLabel,
            title: copy.accountHubCatalogTitle,
            description:
              categoriesStatus === "ok"
                ? copy.publicAuthCatalogFallbackDescription
                : formatResource(copy.publicAuthCatalogEmptyMessage, {
                    status: categoriesStatus,
                  }),
            href: "/catalog",
            ctaLabel: copy.memberCrossSurfaceCatalogCta,
          },
        ]),
    ...(productOpportunities.length > 0
      ? productOpportunities.map((product) => {
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
            id: `auth-product-${product.id}`,
            label: campaignLabel,
            title: product.name,
            description:
              savingsPercent !== null
                ? formatResource(copy.publicAuthProductOfferDescription, {
                    campaignLabel,
                    savingsPercent,
                    price: formatMoney(
                      product.priceMinor,
                      product.currency,
                      culture,
                    ),
                  })
                : product.shortDescription ??
                  copy.publicAuthProductFallbackDescription,
            href: `/catalog/${product.slug}`,
            ctaLabel: copy.publicAuthProductCta,
          };
        })
      : [
          {
            id: "auth-product",
            label: copy.publicAuthProductLabel,
            title: copy.publicAuthProductTitle,
            description:
              productsStatus === "ok"
                ? copy.publicAuthProductFallbackDescription
                : formatResource(copy.publicAuthProductEmptyMessage, {
                    status: productsStatus,
                  }),
            href: "/catalog",
            ctaLabel: copy.publicAuthProductCta,
          },
        ]),
  ];

  return (
    <div className="flex flex-col gap-6">
      <PublicContinuationRail
        culture={culture}
        eyebrow={copy.memberCrossSurfaceTitle}
        title={copy.accountHubRouteMapTitle}
        description={formatResource(copy.publicAuthStorefrontWindowMessage, {
          cartStatus: storefrontCartStatus,
          cartLineCount,
          cmsStatus: cmsPagesStatus,
          categoriesStatus,
          productsStatus,
          pageCount: cmsPages.length,
          categoryCount: categories.length,
          productCount: products.length,
        })}
        items={items}
      />
      <section className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <div className="flex items-center justify-between gap-3">
          <p className="text-sm font-semibold text-[var(--color-text-primary)]">
            {copy.publicAuthCampaignTitle}
          </p>
          <Link
            href={localizeHref("/catalog", culture)}
            className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
          >
            {copy.publicAuthCampaignCta}
          </Link>
        </div>
        <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
          {formatResource(copy.publicAuthCampaignMessage, {
            categoryCount: categories.length,
            productCount: products.length,
            categoriesStatus,
            productsStatus,
          })}
        </p>
        {campaignCards.length > 0 ? (
          <div className="mt-4 grid gap-3 lg:grid-cols-2">
            {campaignCards.map((card) => (
              <Link
                key={card.id}
                href={localizeHref(card.href, culture)}
                className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 transition hover:bg-[var(--color-surface-panel)]"
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
                <p className="mt-3 text-sm font-semibold text-[var(--color-brand)]">
                  {card.ctaLabel}
                </p>
              </Link>
            ))}
          </div>
        ) : (
          <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.publicAuthCampaignEmptyMessage, {
              categoriesStatus,
              productsStatus,
            })}
          </p>
        )}
      </section>
    </div>
  );
}
