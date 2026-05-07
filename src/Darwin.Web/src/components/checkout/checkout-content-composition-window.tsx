import Link from "next/link";
import { buildPromotionLaneRouteMapItem } from "@/components/composition-window-promotion-lane";
import { sortProductsByOpportunity } from "@/features/catalog/merchandising";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import type { MemberInvoiceSummary } from "@/features/member-portal/types";
import { formatMoney } from "@/lib/formatting";
import { buildCatalogProductPath, buildCmsPagePath } from "@/lib/entity-paths";
import { buildAppQueryPath, buildLocalizedAuthHref, localizeHref } from "@/lib/locale-routing";
import { formatResource, getCommerceResource } from "@/localization";

type CheckoutContentCompositionWindowProps = {
  culture: string;
  hasMemberSession: boolean;
  canPlaceOrder: boolean;
  addressComplete: boolean;
  hasSelectedShipping: boolean;
  cartHref: string;
  accountHref: string;
  cmsPages: PublicPageSummary[];
  categories: PublicCategorySummary[];
  products: PublicProductSummary[];
  memberInvoices: MemberInvoiceSummary[];
  projectedCheckoutTotalMinor: number;
  currency: string;
};

export function CheckoutContentCompositionWindow({
  culture,
  hasMemberSession,
  canPlaceOrder,
  addressComplete,
  hasSelectedShipping,
  cartHref,
  accountHref,
  cmsPages,
  categories,
  products,
  memberInvoices,
  projectedCheckoutTotalMinor,
  currency,
}: CheckoutContentCompositionWindowProps) {
  const copy = getCommerceResource(culture);
  const strongestPage = cmsPages[0] ?? null;
  const strongestCategory = categories[0] ?? null;
  const strongestProduct = sortProductsByOpportunity(products)[0] ?? null;
  const outstandingInvoice =
    memberInvoices.find((invoice) => invoice.balanceMinor > 0) ?? null;
  const storefrontHref = strongestProduct
    ? buildCatalogProductPath(strongestProduct.slug)
    : strongestCategory
      ? buildAppQueryPath("/catalog", { category: strongestCategory.slug })
      : "/catalog";
  const accountJourneyHref = hasMemberSession
    ? accountHref
    : buildLocalizedAuthHref("/account/sign-in", "/checkout", culture);

  const promotionLaneRouteMapItem = buildPromotionLaneRouteMapItem({
    id: "checkout-composition-route-promotion-lane",
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
      id: "checkout-route-cart",
      label: copy.checkoutCompositionRouteMapCartLabel,
      title: copy.checkoutCompositionRouteMapCartTitle,
      description: copy.checkoutCompositionRouteMapCartDescription,
      href: cartHref,
      ctaLabel: copy.checkoutCompositionRouteMapCartCta,
      meta: formatResource(copy.checkoutCompositionRouteMapCartMeta, {
        total: formatMoney(projectedCheckoutTotalMinor, currency, culture),
      }),
    },
    {
      id: "checkout-route-account",
      label: copy.checkoutCompositionRouteMapAccountLabel,
      title: hasMemberSession
        ? outstandingInvoice
          ? formatResource(copy.checkoutCompositionRouteMapAccountBillingTitle, {
              balance: formatMoney(
                outstandingInvoice.balanceMinor,
                outstandingInvoice.currency,
                culture,
              ),
            })
          : copy.checkoutCompositionRouteMapAccountMemberTitle
        : copy.checkoutCompositionRouteMapAccountGuestTitle,
      description: hasMemberSession
        ? copy.checkoutCompositionRouteMapAccountMemberDescription
        : copy.checkoutCompositionRouteMapAccountGuestDescription,
      href: accountJourneyHref,
      ctaLabel: hasMemberSession
        ? copy.checkoutCompositionRouteMapAccountMemberCta
        : copy.checkoutCompositionRouteMapAccountGuestCta,
      meta: formatResource(copy.checkoutCompositionRouteMapAccountMeta, {
        count: memberInvoices.length,
      }),
    },
    {
      id: "checkout-route-content",
      label: copy.checkoutCompositionRouteMapContentLabel,
      title: strongestPage
        ? formatResource(copy.checkoutCompositionRouteMapContentTitle, {
            title: strongestPage.title,
          })
        : copy.checkoutCompositionRouteMapContentFallbackTitle,
      description:
        strongestPage?.metaDescription ??
        copy.checkoutCompositionRouteMapContentFallbackDescription,
      href: strongestPage ? buildCmsPagePath(strongestPage.slug) : "/cms",
      ctaLabel: copy.checkoutCompositionRouteMapContentCta,
      meta: formatResource(copy.checkoutCompositionRouteMapContentMeta, {
        count: cmsPages.length,
      }),
    },
    {
      id: "checkout-route-commerce",
      label: copy.checkoutCompositionRouteMapCommerceLabel,
      title: strongestProduct
        ? formatResource(copy.checkoutCompositionRouteMapCommerceTitle, {
            product: strongestProduct.name,
          })
        : strongestCategory
          ? formatResource(copy.checkoutCompositionRouteMapCommerceCategoryTitle, {
              category: strongestCategory.name,
            })
          : copy.checkoutCompositionRouteMapCommerceFallbackTitle,
      description: strongestProduct
        ? formatResource(copy.checkoutCompositionRouteMapCommerceDescription, {
            price: formatMoney(
              strongestProduct.priceMinor,
              strongestProduct.currency,
              culture,
            ),
          })
        : strongestCategory?.description ??
          copy.checkoutCompositionRouteMapCommerceFallbackDescription,
      href: storefrontHref,
      ctaLabel: copy.checkoutCompositionRouteMapCommerceCta,
      meta: formatResource(copy.checkoutCompositionRouteMapCommerceMeta, {
        count: products.length,
      }),
    },
    promotionLaneRouteMapItem,
  ];

  return (
    <div className="grid gap-5 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)]">
      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
          {copy.checkoutCompositionJourneyTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.checkoutCompositionJourneyMessage}
        </p>
        <div className="mt-5 grid gap-3">
          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.checkoutCompositionJourneyCurrentLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {copy.checkoutCompositionJourneyCurrentTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.checkoutCompositionJourneyCurrentDescription, {
                addressState: addressComplete
                  ? copy.checkoutCompositionStateReady
                  : copy.checkoutCompositionStateAttention,
                shippingState: hasSelectedShipping
                  ? copy.checkoutCompositionStateReady
                  : copy.checkoutCompositionStateAttention,
              })}
            </p>
            <div className="mt-4">
              <Link
                href={localizeHref("/checkout", culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.checkoutCompositionJourneyCurrentCta}
              </Link>
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.checkoutCompositionJourneyNextLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {canPlaceOrder
                ? copy.checkoutCompositionJourneyNextReadyTitle
                : copy.checkoutCompositionJourneyNextPendingTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {canPlaceOrder
                ? formatResource(copy.checkoutCompositionJourneyNextReadyDescription, {
                    total: formatMoney(projectedCheckoutTotalMinor, currency, culture),
                  })
                : copy.checkoutCompositionJourneyNextPendingDescription}
            </p>
            <div className="mt-4">
              <Link
                href={localizeHref(canPlaceOrder ? "/checkout" : cartHref, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {canPlaceOrder
                  ? copy.checkoutCompositionJourneyNextReadyCta
                  : copy.checkoutCompositionJourneyNextPendingCta}
              </Link>
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.checkoutCompositionJourneyStorefrontLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {strongestProduct
                ? formatResource(copy.checkoutCompositionJourneyStorefrontTitle, {
                    product: strongestProduct.name,
                  })
                : strongestCategory
                  ? formatResource(copy.checkoutCompositionJourneyStorefrontCategoryTitle, {
                      category: strongestCategory.name,
                    })
                  : copy.checkoutCompositionJourneyStorefrontFallbackTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {strongestProduct
                ? formatResource(copy.checkoutCompositionJourneyStorefrontDescription, {
                    price: formatMoney(
                      strongestProduct.priceMinor,
                      strongestProduct.currency,
                      culture,
                    ),
                  })
                : strongestCategory?.description ??
                  copy.checkoutCompositionJourneyStorefrontFallbackDescription}
            </p>
            <div className="mt-4">
              <Link
                href={localizeHref(storefrontHref, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.checkoutCompositionJourneyStorefrontCta}
              </Link>
            </div>
          </article>
        </div>
      </section>

      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
          {copy.checkoutCompositionRouteMapTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.checkoutCompositionRouteMapMessage}
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
                  href={localizeHref(item.href, culture)}
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



