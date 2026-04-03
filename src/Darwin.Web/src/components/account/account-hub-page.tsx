import Link from "next/link";
import { ActivationRecoveryPanel } from "@/components/account/activation-recovery-panel";
import { PublicAuthContinuation } from "@/components/account/public-auth-continuation";
import { PublicAuthReturnSummary } from "@/components/account/public-auth-return-summary";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicCartSummary } from "@/features/cart/types";
import type { PublicPageSummary } from "@/features/cms/types";
import {
  getStrongestProductOpportunity,
  getProductSavingsPercent,
  sortProductsByOpportunity,
} from "@/features/catalog/merchandising";
import { formatMoney } from "@/lib/formatting";
import {
  buildAppQueryPath,
  buildLocalizedAuthHref,
  localizeHref,
} from "@/lib/locale-routing";
import { formatResource, getMemberResource } from "@/localization";

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
  const cartLineCount =
    storefrontCart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0;
  const preferredReturnPath = returnPath || (cartLineCount > 0 ? "/checkout" : "/account");
  const spotlightCmsPage = cmsPages[0];
  const spotlightCategory = categories[0];
  const spotlightProduct = getStrongestProductOpportunity(products);
  const rankedOffers = sortProductsByOpportunity(products).slice(0, 3);
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

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
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

        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {accountCards.map((card) => (
            <article
              key={card.id}
              className="flex h-full flex-col rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]"
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

        <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
            {copy.accountHubReadinessTitle}
          </p>
          <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.accountHubReadinessMessage, {
              cartStatus: storefrontCartStatus,
              cartLineCount,
              cmsStatus: cmsPagesStatus,
              categoriesStatus,
            })}
          </p>
          <dl className="mt-5 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)] sm:grid-cols-2">
            <div className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3">
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
            <div className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3">
              <dt className="font-semibold text-[var(--color-text-primary)]">
                {copy.accountHubReadinessReturnLabel}
              </dt>
              <dd>{preferredReturnPath}</dd>
            </div>
            <div className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3">
              <dt className="font-semibold text-[var(--color-text-primary)]">
                {copy.accountHubReadinessCmsLabel}
              </dt>
              <dd>
                {formatResource(copy.accountHubReadinessCmsValue, {
                  count: cmsPages.length,
                  status: cmsPagesStatus,
                })}
              </dd>
            </div>
            <div className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3">
              <dt className="font-semibold text-[var(--color-text-primary)]">
                {copy.accountHubReadinessCatalogLabel}
              </dt>
              <dd>
                {formatResource(copy.accountHubReadinessCatalogValue, {
                  count: categories.length,
                  status: categoriesStatus,
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

        <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
            {copy.accountHubActionCenterTitle}
          </p>
          <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
            {copy.accountHubActionCenterMessage}
          </p>
          <div className="mt-5 grid gap-4 lg:grid-cols-2 xl:grid-cols-4">
            <article className="rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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

            <article className="rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.accountHubActionCmsLabel}
              </p>
              <h2 className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                {spotlightCmsPage?.title ?? copy.accountHubActionCmsTitle}
              </h2>
              <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                {spotlightCmsPage?.metaDescription ??
                  copy.accountHubActionCmsFallbackDescription}
              </p>
              <div className="mt-4">
                <Link
                  href={localizeHref(
                    spotlightCmsPage ? `/cms/${spotlightCmsPage.slug}` : "/cms",
                    culture,
                  )}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {copy.accountHubActionCmsCta}
                </Link>
              </div>
            </article>

            <article className="rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.accountHubActionCatalogLabel}
              </p>
              <h2 className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                {spotlightCategory?.name ?? copy.accountHubActionCatalogTitle}
              </h2>
              <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                {spotlightCategory?.description ??
                  copy.accountHubActionCatalogFallbackDescription}
              </p>
              <div className="mt-4">
                <Link
                  href={localizeHref(
                    spotlightCategory
                      ? buildAppQueryPath("/catalog", { category: spotlightCategory.slug })
                      : "/catalog",
                    culture,
                  )}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {copy.accountHubActionCatalogCta}
                </Link>
              </div>
            </article>

            <article className="rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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
                    spotlightProduct ? `/catalog/${spotlightProduct.slug}` : "/catalog",
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

        <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.accountHubOfferBoardTitle}
          </p>
          <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.accountHubOfferBoardMessage, {
              productCount: products.length,
              status: productsStatus,
            })}
          </p>
          {rankedOffers.length > 0 ? (
            <div className="mt-5 grid gap-4 lg:grid-cols-3">
              {rankedOffers.map((product) => {
                const savingsPercent = getProductSavingsPercent(product);

                return (
                  <article
                    key={product.id}
                    className="rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                  >
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {copy.accountHubOfferBoardLabel}
                    </p>
                    <h2 className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                      {product.name}
                    </h2>
                    <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {savingsPercent !== null
                        ? formatResource(copy.accountHubOfferBoardDescription, {
                            savingsPercent,
                            price: formatMoney(
                              product.priceMinor,
                              product.currency,
                              culture,
                            ),
                          })
                        : product.shortDescription ??
                          copy.accountHubOfferBoardFallbackDescription}
                    </p>
                    <div className="mt-4">
                      <Link
                        href={localizeHref(`/catalog/${product.slug}`, culture)}
                        className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                      >
                        {copy.accountHubOfferBoardCta}
                      </Link>
                    </div>
                  </article>
                );
              })}
            </div>
          ) : (
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.accountHubOfferBoardEmptyMessage, {
                status: productsStatus,
              })}
            </p>
          )}
        </aside>

        <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
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
