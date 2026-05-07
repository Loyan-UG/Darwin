import Link from "next/link";
import { buildPromotionLaneRouteMapItem } from "@/components/composition-window-promotion-lane";
import { sortProductsByOpportunity } from "@/features/catalog/merchandising";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import type {
  MemberInvoiceSummary,
  MemberOrderSummary,
  MyLoyaltyOverview,
} from "@/features/member-portal/types";
import { formatMoney } from "@/lib/formatting";
import { buildCatalogProductPath, buildCmsPagePath } from "@/lib/entity-paths";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import { formatResource, getCommerceResource } from "@/localization";

type ConfirmationContentCompositionWindowProps = {
  culture: string;
  hasMemberSession: boolean;
  paymentNeedsAttention: boolean;
  orderNumber: string;
  orderGrossMinor: number;
  currency: string;
  memberOrdersHref: string;
  signInHref: string;
  accountHref: string;
  memberOrders: MemberOrderSummary[];
  memberInvoices: MemberInvoiceSummary[];
  memberLoyaltyOverview: MyLoyaltyOverview | null;
  cmsPages: PublicPageSummary[];
  categories: PublicCategorySummary[];
  products: PublicProductSummary[];
};

export function ConfirmationContentCompositionWindow({
  culture,
  hasMemberSession,
  paymentNeedsAttention,
  orderNumber,
  orderGrossMinor,
  currency,
  memberOrdersHref,
  signInHref,
  accountHref,
  memberOrders,
  memberInvoices,
  memberLoyaltyOverview,
  cmsPages,
  categories,
  products,
}: ConfirmationContentCompositionWindowProps) {
  const copy = getCommerceResource(culture);
  const latestMemberOrder = memberOrders[0] ?? null;
  const outstandingInvoice =
    memberInvoices.find((invoice) => invoice.balanceMinor > 0) ??
    memberInvoices[0] ??
    null;
  const loyaltyFocus =
    [...(memberLoyaltyOverview?.accounts ?? [])].sort((left, right) => {
      const leftRank = left.pointsToNextReward ?? Number.MAX_SAFE_INTEGER;
      const rightRank = right.pointsToNextReward ?? Number.MAX_SAFE_INTEGER;
      return leftRank - rightRank;
    })[0] ?? null;
  const strongestPage = cmsPages[0] ?? null;
  const strongestCategory = categories[0] ?? null;
  const strongestProduct = sortProductsByOpportunity(products)[0] ?? null;
  const paymentHref = paymentNeedsAttention ? "/checkout" : memberOrdersHref;
  const accountJourneyHref = hasMemberSession ? memberOrdersHref : signInHref;
  const storefrontHref = strongestProduct
    ? buildCatalogProductPath(strongestProduct.slug)
    : strongestCategory
      ? buildAppQueryPath("/catalog", { category: strongestCategory.slug })
      : "/catalog";
  const promotionLaneRouteMapItem = buildPromotionLaneRouteMapItem({
    id: "confirmation-composition-route-promotion-lane",
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
      id: "confirmation-route-orders",
      label: copy.confirmationCompositionRouteMapOrdersLabel,
      title: hasMemberSession && latestMemberOrder
        ? formatResource(copy.confirmationCompositionRouteMapOrdersTitle, {
            orderNumber: latestMemberOrder.orderNumber,
          })
        : hasMemberSession
          ? copy.confirmationCompositionRouteMapOrdersFallbackTitle
          : formatResource(copy.confirmationCompositionRouteMapOrdersGuestTitle, {
              orderNumber,
            }),
      description: hasMemberSession
        ? copy.confirmationCompositionRouteMapOrdersDescription
        : copy.confirmationCompositionRouteMapOrdersGuestDescription,
      href: accountJourneyHref,
      ctaLabel: hasMemberSession
        ? copy.confirmationCompositionRouteMapOrdersCta
        : copy.confirmationCompositionRouteMapOrdersGuestCta,
      meta: formatResource(copy.confirmationCompositionRouteMapOrdersMeta, {
        count: memberOrders.length,
      }),
    },
    {
      id: "confirmation-route-account",
      label: copy.confirmationCompositionRouteMapAccountLabel,
      title: outstandingInvoice
        ? formatResource(copy.confirmationCompositionRouteMapAccountBillingTitle, {
            balance: formatMoney(
              outstandingInvoice.balanceMinor,
              outstandingInvoice.currency,
              culture,
            ),
          })
        : loyaltyFocus
          ? formatResource(copy.confirmationCompositionRouteMapAccountLoyaltyTitle, {
              business: loyaltyFocus.businessName,
            })
          : copy.confirmationCompositionRouteMapAccountFallbackTitle,
      description: hasMemberSession
        ? copy.confirmationCompositionRouteMapAccountMemberDescription
        : copy.confirmationCompositionRouteMapAccountGuestDescription,
      href: hasMemberSession ? accountHref : signInHref,
      ctaLabel: hasMemberSession
        ? copy.confirmationCompositionRouteMapAccountMemberCta
        : copy.confirmationCompositionRouteMapAccountGuestCta,
      meta: formatResource(copy.confirmationCompositionRouteMapAccountMeta, {
        orderNumber,
      }),
    },
    {
      id: "confirmation-route-content",
      label: copy.confirmationCompositionRouteMapContentLabel,
      title: strongestPage
        ? formatResource(copy.confirmationCompositionRouteMapContentTitle, {
            title: strongestPage.title,
          })
        : copy.confirmationCompositionRouteMapContentFallbackTitle,
      description: strongestPage?.metaDescription ??
        copy.confirmationCompositionRouteMapContentFallbackDescription,
      href: strongestPage ? buildCmsPagePath(strongestPage.slug) : "/cms",
      ctaLabel: copy.confirmationCompositionRouteMapContentCta,
      meta: formatResource(copy.confirmationCompositionRouteMapContentMeta, {
        count: cmsPages.length,
      }),
    },
    {
      id: "confirmation-route-commerce",
      label: copy.confirmationCompositionRouteMapCommerceLabel,
      title: strongestProduct
        ? formatResource(copy.confirmationCompositionRouteMapCommerceTitle, {
            product: strongestProduct.name,
          })
        : strongestCategory
          ? formatResource(copy.confirmationCompositionRouteMapCommerceCategoryTitle, {
              category: strongestCategory.name,
            })
          : copy.confirmationCompositionRouteMapCommerceFallbackTitle,
      description: strongestProduct
        ? formatResource(copy.confirmationCompositionRouteMapCommerceDescription, {
            price: formatMoney(
              strongestProduct.priceMinor,
              strongestProduct.currency,
              culture,
            ),
          })
        : strongestCategory?.description ??
          copy.confirmationCompositionRouteMapCommerceFallbackDescription,
      href: storefrontHref,
      ctaLabel: copy.confirmationCompositionRouteMapCommerceCta,
      meta: formatResource(copy.confirmationCompositionRouteMapCommerceMeta, {
        count: products.length,
      }),
    },
    promotionLaneRouteMapItem,
  ];

  return (
    <div className="grid gap-5 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)]">
      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
          {copy.confirmationCompositionJourneyTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.confirmationCompositionJourneyMessage}
        </p>
        <div className="mt-5 grid gap-3">
          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.confirmationCompositionJourneyPaymentLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {paymentNeedsAttention
                ? copy.confirmationCompositionJourneyPaymentAttentionTitle
                : copy.confirmationCompositionJourneyPaymentStableTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {paymentNeedsAttention
                ? formatResource(copy.confirmationCompositionJourneyPaymentAttentionDescription, {
                    total: formatMoney(orderGrossMinor, currency, culture),
                  })
                : copy.confirmationCompositionJourneyPaymentStableDescription}
            </p>
            <div className="mt-4">
              <Link
                href={localizeHref(paymentHref, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {paymentNeedsAttention
                  ? copy.confirmationCompositionJourneyPaymentAttentionCta
                  : copy.confirmationCompositionJourneyPaymentStableCta}
              </Link>
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.confirmationCompositionJourneyAccountLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {hasMemberSession
                ? copy.confirmationCompositionJourneyAccountMemberTitle
                : copy.confirmationCompositionJourneyAccountGuestTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {hasMemberSession
                ? formatResource(copy.confirmationCompositionJourneyAccountMemberDescription, {
                    orderCount: memberOrders.length,
                  })
                : formatResource(copy.confirmationCompositionJourneyAccountGuestDescription, {
                    orderNumber,
                  })}
            </p>
            <div className="mt-4">
              <Link
                href={localizeHref(accountJourneyHref, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {hasMemberSession
                  ? copy.confirmationCompositionJourneyAccountMemberCta
                  : copy.confirmationCompositionJourneyAccountGuestCta}
              </Link>
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.confirmationCompositionJourneyStorefrontLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {strongestProduct
                ? formatResource(copy.confirmationCompositionJourneyStorefrontTitle, {
                    product: strongestProduct.name,
                  })
                : strongestCategory
                  ? formatResource(copy.confirmationCompositionJourneyStorefrontCategoryTitle, {
                      category: strongestCategory.name,
                    })
                  : copy.confirmationCompositionJourneyStorefrontFallbackTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {strongestProduct
                ? formatResource(copy.confirmationCompositionJourneyStorefrontDescription, {
                    price: formatMoney(
                      strongestProduct.priceMinor,
                      strongestProduct.currency,
                      culture,
                    ),
                  })
                : strongestCategory?.description ??
                  copy.confirmationCompositionJourneyStorefrontFallbackDescription}
            </p>
            <div className="mt-4">
              <Link
                href={localizeHref(storefrontHref, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.confirmationCompositionJourneyStorefrontCta}
              </Link>
            </div>
          </article>
        </div>
      </section>

      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
          {copy.confirmationCompositionRouteMapTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.confirmationCompositionRouteMapMessage}
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



