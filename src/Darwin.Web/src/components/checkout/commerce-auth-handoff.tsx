import Link from "next/link";
import { StorefrontCampaignBoard } from "@/components/storefront/storefront-campaign-board";
import type { PublicProductSummary } from "@/features/catalog/types";
import type { PublicCartSummary } from "@/features/cart/types";
import { summarizeCatalogPromotionLanes } from "@/features/catalog/promotion-lanes";
import { buildStorefrontOfferCards } from "@/features/storefront/storefront-campaigns";
import { buildStorefrontSpotlightSelections } from "@/features/storefront/storefront-spotlight";
import { formatMoney } from "@/lib/formatting";
import {
  buildAppQueryPath,
  buildLocalizedAuthHref,
  localizeHref,
} from "@/lib/locale-routing";
import {
  formatResource,
  getCommerceResource,
  resolveApiStatusLabel,
} from "@/localization";

type CommerceAuthHandoffProps = {
  culture: string;
  cart: PublicCartSummary;
  returnPath: string;
  routeKey: "cart" | "checkout";
  products: PublicProductSummary[];
  productsStatus: string;
};

export function CommerceAuthHandoff({
  culture,
  cart,
  returnPath,
  routeKey,
  products,
  productsStatus,
}: CommerceAuthHandoffProps) {
  const copy = getCommerceResource(culture);
  const productsStatusLabel =
    resolveApiStatusLabel(productsStatus, copy) ?? productsStatus;
  const cartLineCount = cart.items.reduce((sum, item) => sum + item.quantity, 0);
  const { offerBoardProducts } = buildStorefrontSpotlightSelections({
    cmsPages: [],
    categories: [],
    products,
    offerBoardCount: 3,
  });
  const productOpportunities = buildStorefrontOfferCards(offerBoardProducts, {
    labels: {
      heroOffer: copy.offerCampaignHeroLabel,
      valueOffer: copy.offerCampaignValueLabel,
      priceDrop: copy.offerCampaignPriceDropLabel,
      steadyPick: copy.offerCampaignSteadyLabel,
    },
    formatPrice: (product) =>
      formatMoney(product.priceMinor, product.currency, culture),
    describeWithSavings: (_, input) =>
      formatResource(copy.commerceAuthOfferBoardOfferDescription, {
        campaignLabel: input.campaignLabel,
        savingsPercent: input.savingsPercent,
        price: input.price,
      }),
    describeWithoutSavings: (product) =>
      product.shortDescription ?? copy.commerceAuthOfferBoardFallbackDescription,
    fallbackDescription: copy.commerceAuthOfferBoardFallbackDescription,
  });
  const promotionLaneCards = summarizeCatalogPromotionLanes(offerBoardProducts).map(
    (entry) => {
      const laneLabel =
        entry.lane === "hero-offers"
          ? copy.storefrontWindowPromotionLaneHeroLabel
          : entry.lane === "value-offers"
            ? copy.storefrontWindowPromotionLaneValueLabel
            : entry.lane === "live-offers"
              ? copy.storefrontWindowPromotionLaneLiveOffersLabel
              : copy.storefrontWindowPromotionLaneBaseLabel;
      const href =
        entry.lane === "hero-offers"
          ? buildAppQueryPath("/catalog", {
              visibleState: "offers",
              visibleSort: "offers-first",
              savingsBand: "hero",
            })
          : entry.lane === "value-offers"
            ? buildAppQueryPath("/catalog", {
                visibleState: "offers",
                visibleSort: "offers-first",
                savingsBand: "value",
              })
            : entry.lane === "live-offers"
              ? buildAppQueryPath("/catalog", {
                  visibleState: "offers",
                  visibleSort: "savings-desc",
                })
              : buildAppQueryPath("/catalog", {
                  visibleState: "base",
                  visibleSort: "base-first",
                });

      return {
        id: `commerce-auth-promotion-lane-${entry.lane}`,
        label: copy.storefrontWindowPromotionLaneCardLabel,
        title: entry.anchorProduct
          ? formatResource(copy.storefrontWindowPromotionLaneTitle, {
              lane: laneLabel,
              product: entry.anchorProduct.name,
            })
          : formatResource(copy.storefrontWindowPromotionLaneFallbackTitle, {
              lane: laneLabel,
            }),
        description:
          entry.anchorProduct !== null
            ? formatResource(copy.storefrontWindowPromotionLaneDescription, {
                lane: laneLabel,
                count: entry.count,
                price: formatMoney(
                  entry.anchorProduct.priceMinor,
                  entry.anchorProduct.currency,
                  culture,
                ),
              })
            : formatResource(copy.storefrontWindowPromotionLaneFallbackDescription, {
                lane: laneLabel,
              }),
        href,
        ctaLabel: copy.storefrontWindowPromotionLaneCta,
        meta: formatResource(copy.storefrontWindowPromotionLaneMeta, {
          count: entry.count,
        }),
      };
    },
  );
  const routeTitle =
    routeKey === "checkout"
      ? copy.commerceAuthCheckoutTitle
      : copy.commerceAuthCartTitle;
  const routeDescription =
    routeKey === "checkout"
      ? copy.commerceAuthCheckoutDescription
      : copy.commerceAuthCartDescription;

  return (
    <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
        {copy.commerceAuthEyebrow}
      </p>
      <h2 className="mt-3 text-2xl font-[family-name:var(--font-display)] text-[var(--color-text-primary)]">
        {routeTitle}
      </h2>
      <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
        {routeDescription}
      </p>
      <dl className="mt-5 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)] sm:grid-cols-2">
        <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
          <dt className="font-semibold text-[var(--color-text-primary)]">
            {copy.commerceAuthCartSnapshotLabel}
          </dt>
          <dd>
            {formatResource(copy.commerceAuthCartSnapshotValue, {
              itemCount: cartLineCount,
              total: formatMoney(cart.grandTotalGrossMinor, cart.currency, culture),
            })}
          </dd>
        </div>
        <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
          <dt className="font-semibold text-[var(--color-text-primary)]">
            {copy.commerceAuthReturnPathLabel}
          </dt>
          <dd>{returnPath}</dd>
        </div>
      </dl>
      <div className="mt-6 flex flex-wrap gap-3">
        <Link
          href={buildLocalizedAuthHref("/account/sign-in", returnPath, culture)}
          className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
        >
          {copy.commerceAuthSignInCta}
        </Link>
        <Link
          href={buildLocalizedAuthHref("/account/register", returnPath, culture)}
          className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
        >
          {copy.commerceAuthRegisterCta}
        </Link>
      </div>
      <div className="mt-4 flex flex-wrap gap-3 text-sm font-semibold">
        <Link
          href={buildLocalizedAuthHref("/account/activation", returnPath, culture)}
          className="text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
        >
          {copy.commerceAuthActivationCta}
        </Link>
        <Link
          href={buildLocalizedAuthHref("/account/password", returnPath, culture)}
          className="text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
        >
          {copy.commerceAuthPasswordCta}
        </Link>
      </div>
      <div className="mt-6 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
        <div className="flex items-center justify-between gap-3">
          <p className="text-sm font-semibold text-[var(--color-text-primary)]">
            {copy.commerceAuthOfferBoardTitle}
          </p>
          <Link
            href={localizeHref("/catalog", culture)}
            className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
          >
            {copy.commerceAuthOfferBoardCta}
          </Link>
        </div>
        <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
          {formatResource(copy.commerceAuthOfferBoardMessage, {
            status: productsStatusLabel,
            productCount: products.length,
          })}
        </p>
        {productOpportunities.length > 0 ? (
          <div className="mt-4 grid gap-3 lg:grid-cols-3">
            {productOpportunities.map((product) => {
              return (
                <Link
                  key={product.id}
                  href={localizeHref(product.href, culture)}
                  className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {product.label}
                  </p>
                  <p className="font-semibold text-[var(--color-text-primary)]">
                    {product.title}
                  </p>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {product.description}
                  </p>
                  <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                    {product.price}
                  </p>
                </Link>
              );
            })}
          </div>
        ) : (
          <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.commerceAuthOfferBoardEmptyMessage, {
              status: productsStatusLabel,
            })}
          </p>
        )}
        <div className="mt-6">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-accent)]">
            {copy.storefrontWindowPromotionLaneSectionTitle}
          </p>
          <p className="mt-3 text-sm leading-6 text-[var(--color-text-secondary)]">
            {copy.storefrontWindowPromotionLaneSectionMessage}
          </p>
          <StorefrontCampaignBoard
            culture={culture}
            cards={promotionLaneCards}
            emptyMessage={copy.storefrontWindowPromotionLaneSectionMessage}
          />
        </div>
      </div>
    </aside>
  );
}


