import Link from "next/link";
import { buildPromotionLaneRouteMapItem } from "@/components/composition-window-promotion-lane";
import type { PublicCartSummary } from "@/features/cart/types";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import {
  buildStorefrontCategorySpotlightLinkCards,
  buildStorefrontPageSpotlightCards,
} from "@/features/storefront/storefront-campaigns";
import { buildStorefrontSpotlightSelections } from "@/features/storefront/storefront-spotlight";
import { formatMoney } from "@/lib/formatting";
import { buildCatalogProductPath } from "@/lib/entity-paths";
import { buildLocalizedAuthHref, localizeHref } from "@/lib/locale-routing";
import { formatResource, getMemberResource } from "@/localization";

type AccountHubCompositionWindowProps = {
  culture: string;
  returnPath: string;
  storefrontCart: PublicCartSummary | null;
  cmsPages: PublicPageSummary[];
  categories: PublicCategorySummary[];
  products: PublicProductSummary[];
};

export function AccountHubCompositionWindow({
  culture,
  returnPath,
  storefrontCart,
  cmsPages,
  categories,
  products,
}: AccountHubCompositionWindowProps) {
  const copy = getMemberResource(culture);
  const cartLineCount =
    storefrontCart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0;
  const { spotlightProduct } = buildStorefrontSpotlightSelections({
    cmsPages,
    categories,
    products,
    offerBoardCount: 3,
  });
  const cmsSpotlightCard = buildStorefrontPageSpotlightCards(cmsPages.slice(0, 1), {
    prefix: "account-hub-composition",
    fallbackDescription: copy.accountHubCompositionRouteMapContentFallbackDescription,
  })[0] ?? null;
  const categorySpotlightCard = buildStorefrontCategorySpotlightLinkCards(
    categories.slice(0, 1),
    {
      prefix: "account-hub-composition",
      fallbackDescription:
        copy.accountHubCompositionRouteMapCatalogFallbackDescription,
    },
  )[0] ?? null;
  const currentRouteHref = localizeHref("/account", culture);
  const nextStepHref =
    cartLineCount > 0
      ? localizeHref("/cart", culture)
      : buildLocalizedAuthHref("/account/sign-in", returnPath, culture);
  const contentHref = localizeHref(
    cmsSpotlightCard ? cmsSpotlightCard.href : "/cms",
    culture,
  );
  const storefrontHref = localizeHref(
    spotlightProduct
      ? buildCatalogProductPath(spotlightProduct.slug)
      : categorySpotlightCard
        ? categorySpotlightCard.href
        : "/catalog",
    culture,
  );

  const promotionLaneRouteMapItem = buildPromotionLaneRouteMapItem({
    id: "account-hub-composition-route-promotion-lane",
    products,
    culture,
    copy: {
      cardLabel: copy.accountHubPromotionLaneCardLabel,
      heroLabel: copy.accountHubPromotionLaneHeroLabel,
      valueLabel: copy.accountHubPromotionLaneValueLabel,
      liveOffersLabel: copy.accountHubPromotionLaneLiveOffersLabel,
      baseLabel: copy.accountHubPromotionLaneBaseLabel,
      title: copy.accountHubPromotionLaneTitle,
      fallbackTitle: copy.accountHubPromotionLaneFallbackTitle,
      description: copy.accountHubPromotionLaneDescription,
      fallbackDescription: copy.accountHubPromotionLaneFallbackDescription,
      cta: copy.accountHubPromotionLaneCta,
      meta: copy.accountHubPromotionLaneMeta,
    },
  });
  const routeMapItems = [
    {
      id: "account-hub-route-current",
      label: copy.accountHubCompositionRouteMapCurrentLabel,
      title: copy.accountHubCompositionRouteMapCurrentTitle,
      description: formatResource(copy.accountHubCompositionRouteMapCurrentDescription, {
        returnPath,
      }),
      href: currentRouteHref,
      ctaLabel: copy.accountHubCompositionRouteMapCurrentCta,
      meta: formatResource(copy.accountHubCompositionRouteMapCurrentMeta, {
        returnPath,
      }),
    },
    {
      id: "account-hub-route-next",
      label: copy.accountHubCompositionRouteMapNextLabel,
      title:
        cartLineCount > 0
          ? copy.accountHubCompositionRouteMapNextCartTitle
          : copy.accountHubCompositionRouteMapNextAuthTitle,
      description:
        cartLineCount > 0 && storefrontCart
          ? formatResource(copy.accountHubCompositionRouteMapNextCartDescription, {
              itemCount: cartLineCount,
              total: formatMoney(
                storefrontCart.grandTotalGrossMinor,
                storefrontCart.currency,
                culture,
              ),
            })
          : copy.accountHubCompositionRouteMapNextAuthDescription,
      href: nextStepHref,
      ctaLabel:
        cartLineCount > 0
          ? copy.accountHubCompositionRouteMapNextCartCta
          : copy.accountHubCompositionRouteMapNextAuthCta,
      meta: formatResource(copy.accountHubCompositionRouteMapNextMeta, {
        itemCount: cartLineCount,
      }),
    },
    {
      id: "account-hub-route-content",
      label: copy.accountHubCompositionRouteMapContentLabel,
      title: cmsSpotlightCard?.title ?? copy.accountHubCompositionRouteMapContentFallbackTitle,
      description:
        cmsSpotlightCard?.description ??
        copy.accountHubCompositionRouteMapContentFallbackDescription,
      href: contentHref,
      ctaLabel: copy.accountHubCompositionRouteMapContentCta,
      meta: formatResource(copy.accountHubCompositionRouteMapContentMeta, {
        count: cmsPages.length,
      }),
    },
    {
      id: "account-hub-route-catalog",
      label: copy.accountHubCompositionRouteMapCatalogLabel,
      title: spotlightProduct
        ? formatResource(copy.accountHubCompositionRouteMapCatalogTitle, {
            product: spotlightProduct.name,
          })
        : categorySpotlightCard?.title ?? copy.accountHubCompositionRouteMapCatalogFallbackTitle,
      description: spotlightProduct
        ? formatResource(copy.accountHubCompositionRouteMapCatalogDescription, {
            price: formatMoney(
              spotlightProduct.priceMinor,
              spotlightProduct.currency,
              culture,
            ),
          })
        : categorySpotlightCard?.description ??
          copy.accountHubCompositionRouteMapCatalogFallbackDescription,
      href: storefrontHref,
      ctaLabel: copy.accountHubCompositionRouteMapCatalogCta,
      meta: formatResource(copy.accountHubCompositionRouteMapCatalogMeta, {
        count: products.length,
      }),
    },
    promotionLaneRouteMapItem,
  ];

  return (
    <div className="grid gap-5 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)]">
      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
          {copy.accountHubCompositionJourneyTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.accountHubCompositionJourneyMessage}
        </p>
        <div className="mt-5 grid gap-3">
          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.accountHubCompositionJourneyCurrentLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {copy.accountHubCompositionJourneyCurrentTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.accountHubCompositionJourneyCurrentDescription, {
                returnPath,
              })}
            </p>
            <div className="mt-4">
              <Link
                href={currentRouteHref}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.accountHubCompositionJourneyCurrentCta}
              </Link>
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.accountHubCompositionJourneyNextLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {cartLineCount > 0
                ? copy.accountHubCompositionJourneyNextCartTitle
                : copy.accountHubCompositionJourneyNextAuthTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {cartLineCount > 0 && storefrontCart
                ? formatResource(copy.accountHubCompositionJourneyNextCartDescription, {
                    itemCount: cartLineCount,
                    total: formatMoney(
                      storefrontCart.grandTotalGrossMinor,
                      storefrontCart.currency,
                      culture,
                    ),
                  })
                : copy.accountHubCompositionJourneyNextAuthDescription}
            </p>
            <div className="mt-4">
              <Link
                href={nextStepHref}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {cartLineCount > 0
                  ? copy.accountHubCompositionJourneyNextCartCta
                  : copy.accountHubCompositionJourneyNextAuthCta}
              </Link>
            </div>
          </article>

          <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.accountHubCompositionJourneyStorefrontLabel}
            </p>
            <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
              {spotlightProduct
                ? formatResource(copy.accountHubCompositionJourneyStorefrontTitle, {
                    product: spotlightProduct.name,
                  })
                : categorySpotlightCard?.title ??
                  copy.accountHubCompositionJourneyStorefrontFallbackTitle}
            </p>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {spotlightProduct
                ? formatResource(copy.accountHubCompositionJourneyStorefrontDescription, {
                    price: formatMoney(
                      spotlightProduct.priceMinor,
                      spotlightProduct.currency,
                      culture,
                    ),
                  })
                : categorySpotlightCard?.description ??
                  copy.accountHubCompositionJourneyStorefrontFallbackDescription}
            </p>
            <div className="mt-4">
              <Link
                href={storefrontHref}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.accountHubCompositionJourneyStorefrontCta}
              </Link>
            </div>
          </article>
        </div>
      </section>

      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
          {copy.accountHubCompositionRouteMapTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.accountHubCompositionRouteMapMessage}
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



