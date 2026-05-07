import Link from "next/link";
import { buildPromotionLaneRouteMapItem } from "@/components/composition-window-promotion-lane";
import { sortProductsByOpportunity } from "@/features/catalog/merchandising";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import { formatMoney } from "@/lib/formatting";
import { buildCatalogProductPath, buildCmsPagePath } from "@/lib/entity-paths";
import { buildAppQueryPath, buildLocalizedAuthHref, localizeHref } from "@/lib/locale-routing";
import { formatResource, getCommerceResource } from "@/localization";

type CartContentCompositionWindowProps = {
  culture: string;
  hasMemberSession: boolean;
  itemCount: number;
  grandTotalMinor: number;
  currency: string;
  checkoutHref: string;
  cmsPages: PublicPageSummary[];
  categories: PublicCategorySummary[];
  products: PublicProductSummary[];
};

export function CartContentCompositionWindow({
  culture,
  hasMemberSession,
  itemCount,
  grandTotalMinor,
  currency,
  checkoutHref,
  cmsPages,
  categories,
  products,
}: CartContentCompositionWindowProps) {
  const copy = getCommerceResource(culture);
  const strongestPage = cmsPages[0] ?? null;
  const strongestCategory = categories[0] ?? null;
  const strongestProduct = sortProductsByOpportunity(products)[0] ?? null;
  const currentCartHref = localizeHref("/cart", culture);
  const accountHref = hasMemberSession
    ? localizeHref("/account", culture)
    : buildLocalizedAuthHref("/account/sign-in", "/cart", culture);
  const storefrontPath = strongestProduct
    ? buildCatalogProductPath(strongestProduct.slug)
    : strongestCategory
      ? buildAppQueryPath("/catalog", { category: strongestCategory.slug })
      : "/catalog";
  const storefrontHref = localizeHref(storefrontPath, culture);
  const contentHref = localizeHref(
    strongestPage ? buildCmsPagePath(strongestPage.slug) : "/cms",
    culture,
  );

  const promotionLaneRouteMapItem = buildPromotionLaneRouteMapItem({
    id: "cart-composition-route-promotion-lane",
    products,
    culture,
    copy: {
      cardLabel: copy.storefrontWindowPromotionLaneCardLabel,
      heroLabel: copy.storefrontWindowPromotionLaneHeroLabel,
      valueLabel: copy.storefrontWindowPromotionLaneValueLabel,
      liveOffersLabel: copy.storefrontWindowPromotionLaneLiveOffersLabel,
      baseLabel: copy.storefrontWindowPromotionLaneBaseLabel,
      title: copy.storefrontWindowPromotionLaneTitle,
      fallbackTitle: copy.storefrontWindowPromotionLaneFallbackTitle,
      description: copy.storefrontWindowPromotionLaneDescription,
      fallbackDescription: copy.storefrontWindowPromotionLaneFallbackDescription,
      cta: copy.storefrontWindowPromotionLaneCta,
      meta: copy.storefrontWindowPromotionLaneMeta,
    },
  });
  const routeMapItems = [
    {
      id: "cart-route-current",
      label: copy.cartCompositionRouteMapCurrentLabel,
      title: copy.cartCompositionRouteMapCurrentTitle,
      description: copy.cartCompositionRouteMapCurrentDescription,
      href: currentCartHref,
      ctaLabel: copy.cartCompositionRouteMapCurrentCta,
      meta: formatResource(copy.cartCompositionRouteMapCurrentMeta, {
        count: itemCount,
      }),
    },
    {
      id: "cart-route-checkout",
      label: copy.cartCompositionRouteMapCheckoutLabel,
      title: copy.cartCompositionRouteMapCheckoutTitle,
      description: formatResource(copy.cartCompositionRouteMapCheckoutDescription, {
        total: formatMoney(grandTotalMinor, currency, culture),
      }),
      href: checkoutHref,
      ctaLabel: copy.cartCompositionRouteMapCheckoutCta,
      meta: formatResource(copy.cartCompositionRouteMapCheckoutMeta, {
        total: formatMoney(grandTotalMinor, currency, culture),
      }),
    },
    {
      id: "cart-route-account",
      label: copy.cartCompositionRouteMapAccountLabel,
      title: hasMemberSession
        ? copy.cartCompositionRouteMapAccountMemberTitle
        : copy.cartCompositionRouteMapAccountGuestTitle,
      description: hasMemberSession
        ? copy.cartCompositionRouteMapAccountMemberDescription
        : copy.cartCompositionRouteMapAccountGuestDescription,
      href: accountHref,
      ctaLabel: hasMemberSession
        ? copy.cartCompositionRouteMapAccountMemberCta
        : copy.cartCompositionRouteMapAccountGuestCta,
      meta: formatResource(copy.cartCompositionRouteMapAccountMeta, {
        count: itemCount,
      }),
    },
    {
      id: "cart-route-content",
      label: copy.cartCompositionRouteMapContentLabel,
      title: strongestPage
        ? formatResource(copy.cartCompositionRouteMapContentTitle, {
            title: strongestPage.title,
          })
        : copy.cartCompositionRouteMapContentFallbackTitle,
      description:
        strongestPage?.metaDescription ??
        copy.cartCompositionRouteMapContentFallbackDescription,
      href: contentHref,
      ctaLabel: copy.cartCompositionRouteMapContentCta,
      meta: formatResource(copy.cartCompositionRouteMapContentMeta, {
        count: cmsPages.length,
      }),
    },
    {
      id: "cart-route-commerce",
      label: copy.cartCompositionRouteMapCommerceLabel,
      title: strongestProduct
        ? formatResource(copy.cartCompositionRouteMapCommerceTitle, {
            product: strongestProduct.name,
          })
        : strongestCategory
          ? formatResource(copy.cartCompositionRouteMapCommerceCategoryTitle, {
              category: strongestCategory.name,
            })
          : copy.cartCompositionRouteMapCommerceFallbackTitle,
      description: strongestProduct
        ? formatResource(copy.cartCompositionRouteMapCommerceDescription, {
            price: formatMoney(
              strongestProduct.priceMinor,
              strongestProduct.currency,
              culture,
            ),
          })
        : strongestCategory?.description ??
          copy.cartCompositionRouteMapCommerceFallbackDescription,
      href: storefrontHref,
      ctaLabel: copy.cartCompositionRouteMapCommerceCta,
      meta: formatResource(copy.cartCompositionRouteMapCommerceMeta, {
        count: products.length,
      }),
    },
    promotionLaneRouteMapItem,
  ];

  return (
    <div className="grid gap-5 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)]">
      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
          {copy.cartCompositionJourneyTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.cartCompositionJourneyMessage}
        </p>
        <div className="mt-5 grid gap-3">
          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.cartCompositionJourneyCurrentLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {copy.cartCompositionJourneyCurrentTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.cartCompositionJourneyCurrentDescription, {
                count: itemCount,
                total: formatMoney(grandTotalMinor, currency, culture),
              })}
            </p>
            <div className="mt-4">
              <Link
                href={currentCartHref}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.cartCompositionJourneyCurrentCta}
              </Link>
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.cartCompositionJourneyNextLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {hasMemberSession
                ? copy.cartCompositionJourneyNextMemberTitle
                : copy.cartCompositionJourneyNextGuestTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {hasMemberSession
                ? formatResource(copy.cartCompositionJourneyNextMemberDescription, {
                    total: formatMoney(grandTotalMinor, currency, culture),
                  })
                : copy.cartCompositionJourneyNextGuestDescription}
            </p>
            <div className="mt-4">
              <Link
                href={checkoutHref}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {hasMemberSession
                  ? copy.cartCompositionJourneyNextMemberCta
                  : copy.cartCompositionJourneyNextGuestCta}
              </Link>
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.cartCompositionJourneyStorefrontLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {strongestProduct
                ? formatResource(copy.cartCompositionJourneyStorefrontTitle, {
                    product: strongestProduct.name,
                  })
                : strongestCategory
                  ? formatResource(copy.cartCompositionJourneyStorefrontCategoryTitle, {
                      category: strongestCategory.name,
                    })
                  : copy.cartCompositionJourneyStorefrontFallbackTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {strongestProduct
                ? formatResource(copy.cartCompositionJourneyStorefrontDescription, {
                    price: formatMoney(
                      strongestProduct.priceMinor,
                      strongestProduct.currency,
                      culture,
                    ),
                  })
                : strongestCategory?.description ??
                  copy.cartCompositionJourneyStorefrontFallbackDescription}
            </p>
            <div className="mt-4">
              <Link
                href={storefrontHref}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.cartCompositionJourneyStorefrontCta}
              </Link>
            </div>
          </article>
        </div>
      </section>

      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
          {copy.cartCompositionRouteMapTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.cartCompositionRouteMapMessage}
        </p>
        <div className="mt-5 grid gap-3">
          {routeMapItems.map((item) => (
            <article
              key={item.id}
              className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
            >
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {item.label}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {item.title}
              </p>
              <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                {item.description}
              </p>
              <p className="mt-3 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {item.meta}
              </p>
              <div className="mt-4">
                <Link
                  href={item.href}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {item.ctaLabel}
                </Link>
              </div>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
}



