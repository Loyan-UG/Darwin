import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import {
  applyCartCouponAction,
  removeCartItemAction,
  updateCartQuantityAction,
} from "@/features/cart/actions";
import type { CartViewModel } from "@/features/cart/server/get-cart-view-model";
import type { PublicProductSummary } from "@/features/catalog/types";
import {
  buildCheckoutDraftSearch,
  toCheckoutDraftFromMemberAddress,
} from "@/features/checkout/helpers";
import type {
  MemberAddress,
  MemberCustomerProfile,
  MemberPreferences,
} from "@/features/member-portal/types";
import {
  getCommerceResource,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { buildCatalogProductPath } from "@/lib/entity-paths";
import { formatMoney } from "@/lib/formatting";
import { localizeHref, sanitizeAppPath } from "@/lib/locale-routing";
import { toWebApiUrl } from "@/lib/webapi-url";

type CartPageProps = {
  culture: string;
  model: CartViewModel;
  memberAddresses: MemberAddress[];
  memberAddressesStatus: string;
  memberProfile: MemberCustomerProfile | null;
  memberProfileStatus: string;
  memberPreferences: MemberPreferences | null;
  memberPreferencesStatus: string;
  hasMemberSession: boolean;
  cartStatus?: string;
  cartError?: string;
  followUpProducts?: PublicProductSummary[];
};

function getStatusMessage(status: string | undefined, copy: ReturnType<typeof getCommerceResource>) {
  switch (status) {
    case "added":
      return copy.cartItemAdded;
    case "updated":
      return copy.cartQuantityUpdated;
    case "removed":
      return copy.cartItemRemoved;
    case "coupon-applied":
      return copy.cartCouponApplied;
    case "coupon-cleared":
      return copy.cartCouponCleared;
    default:
      return undefined;
  }
}

export function CartPage({
  culture,
  model,
  memberAddresses,
  hasMemberSession,
  cartStatus,
  cartError,
  followUpProducts = [],
}: CartPageProps) {
  const copy = getCommerceResource(culture);
  const cart = model.cart;
  const statusMessage = getStatusMessage(cartStatus, copy);
  const resolvedCartError = resolveLocalizedQueryMessage(cartError, copy);
  const resolvedModelMessage = resolveLocalizedQueryMessage(model.message, copy);
  const preferredCheckoutAddress =
    memberAddresses.find((address) => address.isDefaultShipping) ??
    memberAddresses.find((address) => address.isDefaultBilling) ??
    memberAddresses[0] ??
    null;
  const checkoutHref = preferredCheckoutAddress
    ? localizeHref(
        `/checkout${buildCheckoutDraftSearch(
          toCheckoutDraftFromMemberAddress(preferredCheckoutAddress),
          { memberAddressId: preferredCheckoutAddress.id },
        )}`,
        culture,
      )
    : localizeHref("/checkout", culture);

  return (
    <section className="mx-auto flex w-full max-w-[1180px] flex-1 px-5 py-10 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <nav
          aria-label={copy.commerceBreadcrumbLabel}
          className="flex flex-wrap items-center gap-2 text-sm text-[var(--color-text-secondary)]"
        >
          <Link href={localizeHref("/", culture)} className="transition hover:text-[var(--color-brand)]">
            {copy.commerceBreadcrumbHome}
          </Link>
          <span>/</span>
          <Link href={localizeHref("/catalog", culture)} className="transition hover:text-[var(--color-brand)]">
            {copy.commerceBreadcrumbCatalog}
          </Link>
          <span>/</span>
          <span className="font-medium text-[var(--color-text-primary)]">
            {copy.commerceBreadcrumbCart}
          </span>
        </nav>

        <header className="rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_48%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.cartHeroEyebrow}
          </p>
          <div className="mt-4 flex flex-wrap items-end justify-between gap-6">
            <div>
              <h1 className="font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
                {copy.cartHeroTitle}
              </h1>
              <p className="mt-4 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)]">
                {copy.cartHeroDescription}
              </p>
            </div>
            {cart ? (
              <div className="rounded-[1rem] bg-white/80 px-5 py-4 text-right shadow-[0_20px_40px_-30px_rgba(58,92,35,0.45)]">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.grandTotalLabel}
                </p>
                <p className="mt-2 text-2xl font-semibold text-[var(--color-text-primary)]">
                  {formatMoney(cart.grandTotalGrossMinor, cart.currency, culture)}
                </p>
              </div>
            ) : null}
          </div>
        </header>

        {statusMessage ? (
          <StatusBanner title={copy.cartUpdatedTitle} message={statusMessage} />
        ) : null}

        {resolvedCartError ? (
          <StatusBanner
            tone="warning"
            title={copy.cartActionFailedTitle}
            message={resolvedCartError}
          />
        ) : null}

        {model.status !== "ok" && model.status !== "empty" ? (
          <StatusBanner
            tone="warning"
          title={copy.cartUnavailableTitle}
          message={resolvedModelMessage ?? copy.cartUnavailableTitle}
          />
        ) : null}

        {!cart || cart.items.length === 0 ? (
          <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-12 text-center shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-text-muted)]">
              {copy.emptyCartEyebrow}
            </p>
            <h2 className="mt-4 font-[family-name:var(--font-display)] text-3xl text-[var(--color-text-primary)]">
              {copy.emptyCartTitle}
            </h2>
            <p className="mx-auto mt-4 max-w-2xl text-base leading-8 text-[var(--color-text-secondary)]">
              {copy.emptyCartDescription}
            </p>
            <Link
              href={localizeHref("/catalog", culture)}
              className="mt-8 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              {copy.continueShopping}
            </Link>
          </div>
        ) : (
          <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_360px]">
            <div className="flex flex-col gap-4">
              {cart.items.map((item) => {
                const imageUrl = toWebApiUrl(item.display?.imageUrl ?? "");
                const itemHref = sanitizeAppPath(item.display?.href, "/catalog");
                const imageAlt =
                  item.display?.imageAlt || item.display?.name || copy.storefrontVariantFallback;

                return (
                  <article
                    key={`${item.variantId}:${item.selectedAddOnValueIdsJson}`}
                    className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-5 shadow-[var(--shadow-panel)]"
                  >
                    <div className="grid gap-5 sm:grid-cols-[112px_minmax(0,1fr)]">
                      <div className="flex h-28 items-center justify-center rounded-[1rem] bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-3">
                        {imageUrl ? (
                          // eslint-disable-next-line @next/next/no-img-element
                          <img src={imageUrl} alt={imageAlt} className="max-h-20 w-auto object-contain" />
                        ) : (
                          <span className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                            {copy.noImage}
                          </span>
                        )}
                      </div>

                      <div className="min-w-0">
                        <div className="flex flex-wrap items-start justify-between gap-4">
                          <div>
                            <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">
                              <Link href={localizeHref(itemHref, culture)} className="transition hover:text-[var(--color-brand)]">
                                {item.display?.name ?? copy.storefrontVariantFallback}
                              </Link>
                            </h2>
                            {(item.selectedAddOns ?? []).length > 0 ? (
                              <div className="mt-2 space-y-1 text-sm text-[var(--color-text-secondary)]">
                                {(item.selectedAddOns ?? []).map((addOn) => (
                                  <p key={addOn.valueId}>
                                    {addOn.optionLabel}: {addOn.valueLabel}
                                  </p>
                                ))}
                              </div>
                            ) : null}
                          </div>
                          <p className="text-lg font-semibold text-[var(--color-text-primary)]">
                            {formatMoney(item.lineGrossMinor, cart.currency, culture)}
                          </p>
                        </div>

                        <div className="mt-5 flex flex-wrap items-end gap-3">
                          <form action={updateCartQuantityAction} className="flex items-end gap-3">
                            <input type="hidden" name="cartId" value={cart.cartId} />
                            <input type="hidden" name="variantId" value={item.variantId} />
                            <input
                              type="hidden"
                              name="selectedAddOnValueIdsJson"
                              value={item.selectedAddOnValueIdsJson}
                            />
                            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                              {copy.cartQuantityAriaLabel}
                              <input
                                name="quantity"
                                type="number"
                                min={1}
                                max={99}
                                defaultValue={item.quantity}
                                className="w-24 rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm outline-none"
                              />
                            </label>
                            <button
                              type="submit"
                              className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold transition hover:bg-[var(--color-surface-panel-strong)]"
                            >
                              {copy.update}
                            </button>
                          </form>

                          <form action={removeCartItemAction}>
                            <input type="hidden" name="cartId" value={cart.cartId} />
                            <input type="hidden" name="variantId" value={item.variantId} />
                            <input
                              type="hidden"
                              name="selectedAddOnValueIdsJson"
                              value={item.selectedAddOnValueIdsJson}
                            />
                            <button
                              type="submit"
                              className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-secondary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                            >
                              {copy.remove}
                            </button>
                          </form>
                        </div>
                      </div>
                    </div>
                  </article>
                );
              })}
            </div>

            <aside className="h-fit rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <h2 className="text-xl font-semibold text-[var(--color-text-primary)]">
                {copy.checkoutSummaryTitle}
              </h2>
              <div className="mt-5 space-y-3 text-sm text-[var(--color-text-secondary)]">
                {cart.couponCode ? (
                  <div className="flex items-center justify-between">
                    <span>{copy.couponLabel}</span>
                    <span>{cart.couponCode}</span>
                  </div>
                ) : null}
                <div className="flex items-center justify-between">
                  <span>{copy.itemsLabel}</span>
                  <span>{cart.items.length}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>{copy.subtotalNetLabel}</span>
                  <span>{formatMoney(cart.subtotalNetMinor, cart.currency, culture)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>{copy.vatTotalLabel}</span>
                  <span>{formatMoney(cart.vatTotalMinor, cart.currency, culture)}</span>
                </div>
                <div className="flex items-center justify-between border-t border-[var(--color-border-soft)] pt-3 text-base font-semibold text-[var(--color-text-primary)]">
                  <span>{copy.grandTotalLabel}</span>
                  <span>{formatMoney(cart.grandTotalGrossMinor, cart.currency, culture)}</span>
                </div>
              </div>

              <form action={applyCartCouponAction} className="mt-6 flex flex-col gap-3">
                <input type="hidden" name="cartId" value={cart.cartId} />
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  {copy.couponCodeLabel}
                  <input
                    name="couponCode"
                    defaultValue={cart.couponCode ?? ""}
                    placeholder={copy.couponPlaceholder}
                    autoComplete="off"
                    maxLength={64}
                    className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
                  />
                </label>
                <button
                  type="submit"
                  className="rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.applyOrClearCoupon}
                </button>
              </form>

              {!hasMemberSession ? (
                <p className="mt-5 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.checkoutSummaryNote}
                </p>
              ) : null}

              <div className="mt-6 flex flex-col gap-3">
                <Link
                  href={checkoutHref}
                  className="inline-flex justify-center rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  {copy.startCheckout}
                </Link>
                <Link
                  href={localizeHref("/catalog", culture)}
                  className="inline-flex justify-center rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.continueShopping}
                </Link>
              </div>
            </aside>
          </div>
        )}

        {cart && cart.items.length > 0 && followUpProducts.length > 0 ? (
          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <div className="flex flex-wrap items-end justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                  {copy.followUpProductsTitle}
                </p>
                <p className="mt-2 max-w-3xl text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.followUpProductsDescription}
                </p>
              </div>
              <Link href={localizeHref("/catalog", culture)} className="text-sm font-semibold text-[var(--color-brand)]">
                {copy.continueShopping}
              </Link>
            </div>
            <div className="mt-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              {followUpProducts.slice(0, 4).map((product) => {
                const productImageUrl = toWebApiUrl(product.primaryImageUrl ?? "");
                return (
                  <article
                    key={product.id}
                    className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-4"
                  >
                    <div className="flex min-h-28 items-center justify-center rounded-[1rem] bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-3">
                      {productImageUrl ? (
                        // eslint-disable-next-line @next/next/no-img-element
                        <img src={productImageUrl} alt={product.name} className="max-h-20 w-auto object-contain" />
                      ) : (
                        <span className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                          {copy.noImage}
                        </span>
                      )}
                    </div>
                    <h3 className="mt-4 text-base font-semibold text-[var(--color-text-primary)]">
                      <Link href={localizeHref(buildCatalogProductPath(product.slug), culture)} className="transition hover:text-[var(--color-brand)]">
                        {product.name}
                      </Link>
                    </h3>
                    <p className="mt-3 text-sm font-semibold text-[var(--color-text-primary)]">
                      {formatMoney(product.priceMinor, product.currency, culture)}
                    </p>
                  </article>
                );
              })}
            </div>
          </section>
        ) : null}
      </div>
    </section>
  );
}
