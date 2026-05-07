import Link from "next/link";
import { AccountHubCompositionWindow } from "@/components/account/account-hub-composition-window";
import { ActivationRecoveryPanel } from "@/components/account/activation-recovery-panel";
import { PublicAuthContinuation } from "@/components/account/public-auth-continuation";
import { PublicAuthReturnSummary } from "@/components/account/public-auth-return-summary";
import { StorefrontCampaignBoard } from "@/components/storefront/storefront-campaign-board";
import { StorefrontOfferBoard } from "@/components/storefront/storefront-offer-board";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicCartSummary } from "@/features/cart/types";
import type { PublicPageSummary } from "@/features/cms/types";
import { summarizeCatalogPromotionLanes } from "@/features/catalog/promotion-lanes";
import {
  buildStorefrontCategorySpotlightLinkCards,
  buildStorefrontOfferCards,
  buildStorefrontPageSpotlightCards,
} from "@/features/storefront/storefront-campaigns";
import { buildStorefrontSpotlightSelections } from "@/features/storefront/storefront-spotlight";
import { formatMoney } from "@/lib/formatting";
import { buildCatalogProductPath } from "@/lib/entity-paths";
import {
  buildLocalizedAuthHref,
  localizeHref,
} from "@/lib/locale-routing";
import {
  formatResource,
  getMemberResource,
  resolveApiStatusLabel,
} from "@/localization";

type AccountHubPageProps = {
  culture: string;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
  storefrontCart: PublicCartSummary | null;
  storefrontCartStatus: string;
  returnPath?: string;
};

export function AccountHubPage({
  culture,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
  storefrontCart,
  storefrontCartStatus,
  returnPath,
}: AccountHubPageProps) {
  const copy = getMemberResource(culture);
  const localizedCmsPagesStatus =
    resolveApiStatusLabel(cmsPagesStatus, copy) ?? cmsPagesStatus;
  const localizedCategoriesStatus =
    resolveApiStatusLabel(categoriesStatus, copy) ?? categoriesStatus;
  const localizedProductsStatus =
    resolveApiStatusLabel(productsStatus, copy) ?? productsStatus;
  const localizedStorefrontCartStatus = resolveApiStatusLabel(
    storefrontCartStatus,
    copy,
  ) ?? storefrontCartStatus;
  const cartLineCount =
    storefrontCart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0;
  const preferredReturnPath = returnPath || (cartLineCount > 0 ? "/checkout" : "/account");
  const {
    spotlightProduct,
    offerBoardProducts: rankedOffers,
  } = buildStorefrontSpotlightSelections({
    cmsPages,
    categories,
    products,
    offerBoardCount: 3,
  });
  const offerBoardCards = buildStorefrontOfferCards(rankedOffers, {
    labels: {
      heroOffer: copy.offerCampaignHeroLabel,
      valueOffer: copy.offerCampaignValueLabel,
      priceDrop: copy.offerCampaignPriceDropLabel,
      steadyPick: copy.offerCampaignSteadyLabel,
    },
    formatPrice: (product) =>
      formatMoney(product.priceMinor, product.currency, culture),
    describeWithSavings: (_, input) =>
      formatResource(copy.accountHubOfferBoardDescription, {
        campaignLabel: input.campaignLabel,
        savingsPercent: input.savingsPercent,
        price: input.price,
      }),
    describeWithoutSavings: (product) =>
      product.shortDescription ?? copy.accountHubOfferBoardFallbackDescription,
    fallbackDescription: copy.accountHubOfferBoardFallbackDescription,
    ctaLabel: copy.accountHubOfferBoardCta,
  });
  const promotionLaneCards = summarizeCatalogPromotionLanes(rankedOffers).map(
    (entry) => {
      const laneLabel =
        entry.lane === "hero-offers"
          ? copy.accountHubPromotionLaneHeroLabel
          : entry.lane === "value-offers"
            ? copy.accountHubPromotionLaneValueLabel
            : entry.lane === "live-offers"
              ? copy.accountHubPromotionLaneLiveOffersLabel
              : copy.accountHubPromotionLaneBaseLabel;
      const href =
        entry.lane === "hero-offers"
          ? "/catalog?visibleState=offers&visibleSort=offers-first&savingsBand=hero"
          : entry.lane === "value-offers"
            ? "/catalog?visibleState=offers&visibleSort=offers-first&savingsBand=value"
            : entry.lane === "live-offers"
              ? "/catalog?visibleState=offers&visibleSort=savings-desc"
              : "/catalog?visibleState=base&visibleSort=base-first";

      return {
        id: `account-hub-promotion-lane-${entry.lane}`,
        label: copy.accountHubPromotionLaneCardLabel,
        title: entry.anchorProduct
          ? formatResource(copy.accountHubPromotionLaneTitle, {
              lane: laneLabel,
              product: entry.anchorProduct.name,
            })
          : formatResource(copy.accountHubPromotionLaneFallbackTitle, {
              lane: laneLabel,
            }),
        description:
          entry.anchorProduct !== null
            ? formatResource(copy.accountHubPromotionLaneDescription, {
                lane: laneLabel,
                count: entry.count,
                price: formatMoney(
                  entry.anchorProduct.priceMinor,
                  entry.anchorProduct.currency,
                  culture,
                ),
              })
            : formatResource(copy.accountHubPromotionLaneFallbackDescription, {
                lane: laneLabel,
              }),
        href,
        ctaLabel: copy.accountHubPromotionLaneCta,
        meta: formatResource(copy.accountHubPromotionLaneMeta, {
          count: entry.count,
        }),
      };
    },
  );
  const cmsSpotlightCards = buildStorefrontPageSpotlightCards(cmsPages.slice(0, 1), {
    prefix: "account-hub",
    fallbackDescription: copy.accountHubActionCmsFallbackDescription,
  });
  const categorySpotlightCards = buildStorefrontCategorySpotlightLinkCards(
    categories.slice(0, 1),
    {
      prefix: "account-hub",
      fallbackDescription: copy.accountHubActionCatalogFallbackDescription,
    },
  );
  const cmsSpotlightCard = cmsSpotlightCards[0] ?? null;
  const categorySpotlightCard = categorySpotlightCards[0] ?? null;
  const accountCards = [
    {
      id: "sign-in",
      eyebrow: copy.cardSignInEyebrow,
      title: copy.cardSignInTitle,
      description: copy.cardSignInDescription,
      href: buildLocalizedAuthHref("/account/sign-in", preferredReturnPath, culture),
      ctaLabel: copy.cardSignInCta,
    },
    {
      id: "register",
      eyebrow: copy.cardRegisterEyebrow,
      title: copy.cardRegisterTitle,
      description: copy.cardRegisterDescription,
      href: buildLocalizedAuthHref("/account/register", preferredReturnPath, culture),
      ctaLabel: copy.cardRegisterCta,
    },
    {
      id: "activation",
      eyebrow: copy.cardActivationEyebrow,
      title: copy.cardActivationTitle,
      description: copy.cardActivationDescription,
      href: buildLocalizedAuthHref("/account/activation", preferredReturnPath, culture),
      ctaLabel: copy.cardActivationCta,
    },
    {
      id: "password",
      eyebrow: copy.cardPasswordEyebrow,
      title: copy.cardPasswordTitle,
      description: copy.cardPasswordDescription,
      href: buildLocalizedAuthHref("/account/password", preferredReturnPath, culture),
      ctaLabel: copy.cardPasswordCta,
    },
  ];
  const sectionLinks = [
    { id: "account-entry", label: copy.accountHubTitle },
    { id: "account-readiness", label: copy.accountHubReadinessTitle },
    { id: "account-composition", label: copy.accountHubCompositionJourneyTitle },
    { id: "account-actions", label: copy.accountHubActionCenterTitle },
    { id: "account-offers", label: copy.accountHubOfferBoardTitle },
  ];

  return (
    <section className="mx-auto flex w-full max-w-[1320px] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div
          id="account-entry"
          className="scroll-mt-28 overflow-hidden rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_42%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8 sm:py-10"
        >
          <div className="grid gap-8 lg:grid-cols-[minmax(0,1.2fr)_320px] lg:items-end">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
                {copy.accountHubEyebrow}
              </p>
              <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
                {copy.accountHubTitle}
              </h1>
              <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                {copy.accountHubDescription}
              </p>
            </div>
            <div className="grid gap-3 sm:grid-cols-3 lg:grid-cols-1">
              <article className="rounded-[1rem] border border-white/70 bg-white/80 px-5 py-4 shadow-[0_20px_40px_-28px_rgba(58,92,35,0.45)] backdrop-blur">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.accountHubReadinessCartLabel}
                </p>
                <p className="mt-2 text-xl font-semibold text-[var(--color-text-primary)]">
                  {cartLineCount}
                </p>
              </article>
              <article className="rounded-[1rem] border border-white/70 bg-white/80 px-5 py-4 shadow-[0_20px_40px_-28px_rgba(58,92,35,0.45)] backdrop-blur">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.accountHubReadinessCmsLabel}
                </p>
                <p className="mt-2 text-xl font-semibold text-[var(--color-text-primary)]">
                  {cmsPages.length}
                </p>
              </article>
              <article className="rounded-[1rem] border border-white/70 bg-[linear-gradient(135deg,rgba(57,116,47,0.94),rgba(255,145,77,0.92))] px-5 py-4 text-white shadow-[0_20px_40px_-28px_rgba(58,92,35,0.55)]">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/78">
                  {copy.accountHubReadinessReturnLabel}
                </p>
                <p className="mt-2 text-sm font-semibold text-white">
                  {preferredReturnPath}
                </p>
              </article>
            </div>
          </div>
        </div>

        <section className="sticky top-4 z-10 rounded-[1rem] border border-[#dce6cf] bg-[color:color-mix(in_srgb,white_84%,#eff7e9_16%)] px-6 py-5 shadow-[0_24px_54px_-36px_rgba(58,92,35,0.32)] backdrop-blur">
          <div className="flex flex-wrap gap-2">
            {sectionLinks.map((section) => (
              <a
                key={section.id}
                href={`#${section.id}`}
                className="inline-flex items-center rounded-full border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {section.label}
              </a>
            ))}
          </div>
        </section>

        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
          {accountCards.map((card) => (
            <article
              key={card.id}
              className="flex h-full flex-col rounded-[1rem] border border-[#dce6cf] bg-white p-6 shadow-[0_24px_54px_-34px_rgba(58,92,35,0.26)] transition hover:-translate-y-0.5 hover:shadow-[0_30px_60px_-34px_rgba(58,92,35,0.3)]"
            >
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {card.eyebrow}
              </p>
              <h2 className="mt-4 text-2xl font-semibold text-[var(--color-text-primary)]">
                {card.title}
              </h2>
              <p className="mt-4 flex-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                {card.description}
              </p>
              <div className="mt-6">
                <Link
                  href={localizeHref(card.href, culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {card.ctaLabel}
                </Link>
              </div>
            </article>
          ))}
        </div>

        <aside
          id="account-readiness"
          className="scroll-mt-28 rounded-[1rem] border border-[#dce6cf] bg-[linear-gradient(160deg,#ffffff_0%,#f7fbef_100%)] px-6 py-8 shadow-[0_24px_54px_-34px_rgba(58,92,35,0.25)] sm:px-8"
        >
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
            {copy.accountHubReadinessTitle}
          </p>
          <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.accountHubReadinessMessage, {
              cartStatus: localizedStorefrontCartStatus,
              cartLineCount,
              cmsStatus: localizedCmsPagesStatus,
              categoriesStatus: localizedCategoriesStatus,
            })}
          </p>
          <dl className="mt-5 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)] sm:grid-cols-2">
            <div className="rounded-2xl border border-[#e3ebd6] bg-white px-4 py-3 shadow-[0_16px_30px_-28px_rgba(58,92,35,0.22)]">
              <dt className="font-semibold text-[var(--color-text-primary)]">
                {copy.accountHubReadinessCartLabel}
              </dt>
              <dd>
                {storefrontCart
                  ? formatResource(copy.accountHubReadinessCartValue, {
                      itemCount: cartLineCount,
                      total: formatMoney(
                        storefrontCart.grandTotalGrossMinor,
                        storefrontCart.currency,
                        culture,
                      ),
                    })
                  : copy.accountHubReadinessCartEmpty}
              </dd>
            </div>
            <div className="rounded-2xl border border-[#e3ebd6] bg-white px-4 py-3 shadow-[0_16px_30px_-28px_rgba(58,92,35,0.22)]">
              <dt className="font-semibold text-[var(--color-text-primary)]">
                {copy.accountHubReadinessReturnLabel}
              </dt>
              <dd>{preferredReturnPath}</dd>
            </div>
            <div className="rounded-2xl border border-[#e3ebd6] bg-white px-4 py-3 shadow-[0_16px_30px_-28px_rgba(58,92,35,0.22)]">
              <dt className="font-semibold text-[var(--color-text-primary)]">
                {copy.accountHubReadinessCmsLabel}
              </dt>
              <dd>
                {formatResource(copy.accountHubReadinessCmsValue, {
                  count: cmsPages.length,
                  status: localizedCmsPagesStatus,
                })}
              </dd>
            </div>
            <div className="rounded-2xl border border-[#e3ebd6] bg-white px-4 py-3 shadow-[0_16px_30px_-28px_rgba(58,92,35,0.22)]">
              <dt className="font-semibold text-[var(--color-text-primary)]">
                {copy.accountHubReadinessCatalogLabel}
              </dt>
              <dd>
                {formatResource(copy.accountHubReadinessCatalogValue, {
                  count: categories.length,
                  status: localizedCategoriesStatus,
                })}
              </dd>
            </div>
          </dl>
          <div className="mt-6 flex flex-wrap gap-3">
            <Link
              href={buildLocalizedAuthHref("/account/sign-in", preferredReturnPath, culture)}
              className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              {copy.accountHubReadinessPrimaryCta}
            </Link>
            {cartLineCount > 0 && (
              <Link
                href={localizeHref("/cart", culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.accountHubReadinessCartCta}
              </Link>
            )}
          </div>
        </aside>

        <PublicAuthReturnSummary
          culture={culture}
          returnPath={preferredReturnPath}
          storefrontCart={storefrontCart}
        />

        <div id="account-composition" className="scroll-mt-28">
          <AccountHubCompositionWindow
            culture={culture}
            returnPath={preferredReturnPath}
            storefrontCart={storefrontCart}
            cmsPages={cmsPages}
            categories={categories}
            products={products}
          />
        </div>

        <aside
          id="account-actions"
          className="scroll-mt-28 rounded-[1rem] border border-[#dce6cf] bg-[linear-gradient(160deg,#ffffff_0%,#fff7ea_100%)] px-6 py-8 shadow-[0_24px_54px_-34px_rgba(58,92,35,0.25)] sm:px-8"
        >
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
            {copy.accountHubActionCenterTitle}
          </p>
          <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
            {copy.accountHubActionCenterMessage}
          </p>
          <div className="mt-5 grid gap-4 lg:grid-cols-2 xl:grid-cols-4">
            <article className="rounded-[1rem] border border-[#e3ebd6] bg-white px-4 py-4 shadow-[0_18px_34px_-28px_rgba(58,92,35,0.24)]">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.accountHubActionCartLabel}
              </p>
              <h2 className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                {copy.accountHubActionCartTitle}
              </h2>
              <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                {storefrontCart
                  ? formatResource(copy.accountHubActionCartDescription, {
                      itemCount: cartLineCount,
                      total: formatMoney(
                        storefrontCart.grandTotalGrossMinor,
                        storefrontCart.currency,
                        culture,
                      ),
                    })
                  : copy.accountHubActionCartFallbackDescription}
              </p>
              <div className="mt-4">
                <Link
                  href={cartLineCount > 0 ? localizeHref("/cart", culture) : buildLocalizedAuthHref("/account/sign-in", preferredReturnPath, culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {cartLineCount > 0
                    ? copy.accountHubActionCartCta
                    : copy.accountHubActionCartFallbackCta}
                </Link>
              </div>
            </article>

            <article className="rounded-[1rem] border border-[#e3ebd6] bg-white px-4 py-4 shadow-[0_18px_34px_-28px_rgba(58,92,35,0.24)]">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.accountHubActionCmsLabel}
              </p>
              <h2 className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                {cmsSpotlightCard?.title ?? copy.accountHubActionCmsTitle}
              </h2>
              <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                {cmsSpotlightCard?.description ??
                  copy.accountHubActionCmsFallbackDescription}
              </p>
              <div className="mt-4">
                <Link
                  href={localizeHref(
                    cmsSpotlightCard ? cmsSpotlightCard.href : "/cms",
                    culture,
                  )}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {copy.accountHubActionCmsCta}
                </Link>
              </div>
            </article>

            <article className="rounded-[1rem] border border-[#e3ebd6] bg-white px-4 py-4 shadow-[0_18px_34px_-28px_rgba(58,92,35,0.24)]">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.accountHubActionCatalogLabel}
              </p>
              <h2 className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                {categorySpotlightCard?.title ?? copy.accountHubActionCatalogTitle}
              </h2>
              <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                {categorySpotlightCard?.description ??
                  copy.accountHubActionCatalogFallbackDescription}
              </p>
              <div className="mt-4">
                <Link
                  href={localizeHref(
                    categorySpotlightCard ? categorySpotlightCard.href : "/catalog",
                    culture,
                  )}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {copy.accountHubActionCatalogCta}
                </Link>
              </div>
            </article>

            <article className="rounded-[1rem] border border-[#e3ebd6] bg-white px-4 py-4 shadow-[0_18px_34px_-28px_rgba(58,92,35,0.24)]">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.accountHubActionProductLabel}
              </p>
              <h2 className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                {spotlightProduct?.name ?? copy.accountHubActionProductTitle}
              </h2>
              <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                {spotlightProduct
                  ? formatResource(copy.accountHubActionProductDescription, {
                      price: formatMoney(
                        spotlightProduct.priceMinor,
                        spotlightProduct.currency,
                        culture,
                      ),
                    })
                  : copy.accountHubActionProductFallbackDescription}
              </p>
              <div className="mt-4">
                <Link
                  href={localizeHref(
                    spotlightProduct ? buildCatalogProductPath(spotlightProduct.slug) : "/catalog",
                    culture,
                  )}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {copy.accountHubActionProductCta}
                </Link>
              </div>
            </article>
          </div>
        </aside>

        <aside
          id="account-offers"
          className="scroll-mt-28 rounded-[1rem] border border-[#dce6cf] bg-[linear-gradient(160deg,#ffffff_0%,#f7fbef_100%)] px-6 py-8 shadow-[0_24px_54px_-34px_rgba(58,92,35,0.25)] sm:px-8"
        >
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.accountHubOfferBoardTitle}
          </p>
          <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.accountHubOfferBoardMessage, {
              productCount: products.length,
              status: localizedProductsStatus,
            })}
          </p>
          <StorefrontOfferBoard
            culture={culture}
            cards={offerBoardCards}
            emptyMessage={formatResource(copy.accountHubOfferBoardEmptyMessage, {
              status: localizedProductsStatus,
            })}
          />
          <div className="mt-8">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.accountHubPromotionLaneSectionTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.accountHubPromotionLaneSectionMessage}
            </p>
            <StorefrontCampaignBoard
              culture={culture}
              cards={promotionLaneCards}
              emptyMessage={copy.accountHubPromotionLaneSectionMessage}
            />
          </div>
        </aside>

        <aside className="rounded-[1rem] border border-[#dce6cf] bg-[linear-gradient(160deg,#ffffff_0%,#fff7ea_100%)] px-6 py-8 shadow-[0_24px_54px_-34px_rgba(58,92,35,0.25)] sm:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.sessionStrategyNoteTitle}
          </p>
          <p className="mt-4 text-base leading-8 text-[var(--color-text-secondary)]">
            {copy.sessionStrategyNoteDescription}
          </p>
        </aside>

        <ActivationRecoveryPanel
          culture={culture}
          returnPath={preferredReturnPath}
        />

        <PublicAuthContinuation
          culture={culture}
          cmsPages={cmsPages}
          cmsPagesStatus={cmsPagesStatus}
          categories={categories}
          categoriesStatus={categoriesStatus}
          products={products}
          productsStatus={productsStatus}
          storefrontCart={storefrontCart}
          storefrontCartStatus={storefrontCartStatus}
        />
      </div>
    </section>
  );
}
