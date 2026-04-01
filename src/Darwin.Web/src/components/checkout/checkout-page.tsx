import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { placeStorefrontOrderAction } from "@/features/checkout/actions";
import { isCheckoutAddressComplete } from "@/features/checkout/helpers";
import type { CheckoutDraft, PublicCheckoutIntent } from "@/features/checkout/types";
import type { CartViewModel } from "@/features/cart/server/get-cart-view-model";
import { formatResource, getCommerceResource } from "@/localization";
import { formatMoney } from "@/lib/formatting";

type CheckoutPageProps = {
  culture: string;
  model: CartViewModel;
  draft: CheckoutDraft;
  intent: PublicCheckoutIntent | null;
  intentStatus: string;
  intentMessage?: string;
  checkoutError?: string;
};

function getFinalTotalMinor(
  intent: PublicCheckoutIntent | null,
  cartTotalMinor: number,
) {
  if (!intent) {
    return cartTotalMinor;
  }

  return intent.grandTotalGrossMinor + intent.selectedShippingTotalMinor;
}

export function CheckoutPage({
  culture,
  model,
  draft,
  intent,
  intentStatus,
  intentMessage,
  checkoutError,
}: CheckoutPageProps) {
  const copy = getCommerceResource(culture);
  const cart = model.cart;
  const addressComplete = isCheckoutAddressComplete(draft);
  const requiresShipping = intent?.requiresShipping ?? true;
  const hasSelectedShipping =
    !requiresShipping ||
    !intent ||
    !intent.shippingOptions.length ||
    Boolean(intent.selectedShippingMethodId || draft.selectedShippingMethodId);
  const canPlaceOrder = Boolean(cart && intent && hasSelectedShipping);

  if (!cart) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
        <div className="flex w-full flex-col gap-6 rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title={copy.checkoutUnavailableTitle}
            message={model.message ?? copy.checkoutUnavailableMessage}
          />
          <div>
            <Link
              href="/catalog"
              className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              {copy.browseCatalog}
            </Link>
          </div>
        </div>
      </section>
    );
  }

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.publicCheckoutEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.checkoutHeroTitle}
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {copy.checkoutHeroDescription}
          </p>
        </div>

        {checkoutError && (
          <StatusBanner
            tone="warning"
            title={copy.checkoutActionFailedTitle}
            message={checkoutError}
          />
        )}

        {!addressComplete && (
          <StatusBanner
            title={copy.addressIncompleteTitle}
            message={copy.addressIncompleteMessage}
          />
        )}

        {intentStatus !== "idle" && intentStatus !== "ok" && (
          <StatusBanner
            tone="warning"
            title={copy.previewDegradedTitle}
            message={intentMessage ?? formatResource(copy.previewDegradedMessage, {
              status: intentStatus,
            })}
          />
        )}

        <div className="grid gap-8 lg:grid-cols-[minmax(0,1.1fr)_360px]">
          <form
            action="/checkout"
            className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)] sm:px-8"
          >
            <div className="grid gap-6">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                  {copy.billingShippingEyebrow}
                </p>
                <h2 className="mt-3 text-3xl font-[family-name:var(--font-display)] text-[var(--color-text-primary)]">
                  {copy.deliveryAddressTitle}
                </h2>
                <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.deliveryAddressDescription}
                </p>
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  {copy.fullNameLabel}
                  <input name="fullName" defaultValue={draft.fullName} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  {copy.companyLabel}
                  <input name="company" defaultValue={draft.company} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
                  {copy.street1Label}
                  <input name="street1" defaultValue={draft.street1} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
                  {copy.street2Label}
                  <input name="street2" defaultValue={draft.street2} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  {copy.postalCodeLabel}
                  <input name="postalCode" defaultValue={draft.postalCode} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  {copy.cityLabel}
                  <input name="city" defaultValue={draft.city} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  {copy.stateLabel}
                  <input name="state" defaultValue={draft.state} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  {copy.countryCodeLabel}
                  <input name="countryCode" defaultValue={draft.countryCode} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal uppercase text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
                  {copy.phoneLabel}
                  <input name="phoneE164" defaultValue={draft.phoneE164} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
              </div>

              {intent?.requiresShipping && intent.shippingOptions.length > 0 && (
                <div className="grid gap-3 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] p-5">
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                      {copy.shippingOptionsTitle}
                    </p>
                    <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {copy.shippingOptionsDescription}
                    </p>
                  </div>
                  {intent.shippingOptions.map((option) => {
                    const isChecked =
                      (draft.selectedShippingMethodId || intent.selectedShippingMethodId) === option.methodId;

                    return (
                      <label
                        key={option.methodId}
                        className="flex cursor-pointer items-start gap-3 rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-4 text-sm text-[var(--color-text-secondary)]"
                      >
                        <input
                          type="radio"
                          name="selectedShippingMethodId"
                          value={option.methodId}
                          defaultChecked={isChecked}
                          className="mt-1"
                        />
                        <span className="flex-1">
                          <span className="block font-semibold text-[var(--color-text-primary)]">
                            {option.name}
                          </span>
                          <span className="block">
                            {option.carrier} / {option.service}
                          </span>
                        </span>
                        <span className="font-semibold text-[var(--color-text-primary)]">
                          {formatMoney(option.priceMinor, option.currency, culture)}
                        </span>
                      </label>
                    );
                  })}
                </div>
              )}

              <div className="flex flex-wrap gap-3">
                <button
                  type="submit"
                  className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  {copy.refreshCheckoutPreview}
                </button>
                <Link
                  href="/cart"
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.backToCart}
                </Link>
              </div>
            </div>
          </form>

          <div className="flex flex-col gap-5">
            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.checkoutSummaryTitle}
              </p>
              <div className="mt-5 space-y-3 text-sm text-[var(--color-text-secondary)]">
                {cart.couponCode ? (
                  <div className="flex items-center justify-between">
                    <span>{copy.couponLabel}</span>
                    <span>{cart.couponCode}</span>
                  </div>
                ) : null}
                <div className="flex items-center justify-between">
                  <span>{copy.cartLinesLabel}</span>
                  <span>{cart.items.length}</span>
                </div>
                {intent ? (
                  <>
                    <div className="flex items-center justify-between">
                      <span>{copy.shipmentMassLabel}</span>
                      <span>{intent.shipmentMass}</span>
                    </div>
                    <div className="flex items-center justify-between">
                      <span>{copy.shippingCountryLabel}</span>
                      <span>{intent.shippingCountryCode ?? (draft.countryCode || copy.unavailable)}</span>
                    </div>
                  </>
                ) : null}
                <div className="flex items-center justify-between">
                  <span>{copy.subtotalNetLabel}</span>
                  <span>{formatMoney(intent?.subtotalNetMinor ?? cart.subtotalNetMinor, cart.currency, culture)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>{copy.vatTotalLabel}</span>
                  <span>{formatMoney(intent?.vatTotalMinor ?? cart.vatTotalMinor, cart.currency, culture)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>{copy.shippingLabel}</span>
                  <span>{formatMoney(intent?.selectedShippingTotalMinor ?? 0, cart.currency, culture)}</span>
                </div>
                <div className="flex items-center justify-between border-t border-[var(--color-border-soft)] pt-3 text-base font-semibold text-[var(--color-text-primary)]">
                  <span>{copy.estimatedGrandTotalLabel}</span>
                  <span>{formatMoney(getFinalTotalMinor(intent, cart.grandTotalGrossMinor), cart.currency, culture)}</span>
                </div>
              </div>

              {intent?.requiresShipping && !hasSelectedShipping && (
                <div className="mt-5">
                  <StatusBanner
                    tone="warning"
                    title={copy.shippingSelectionMissingTitle}
                    message={copy.shippingSelectionMissingMessage}
                  />
                </div>
              )}

              {!intent && (
                <div className="mt-5">
                  <StatusBanner
                    title={copy.noPreviewTitle}
                    message={copy.noPreviewMessage}
                  />
                </div>
              )}

              {intent && !intent.requiresShipping && (
                <div className="mt-5">
                  <StatusBanner
                    title={copy.shippingNotRequiredTitle}
                    message={copy.shippingNotRequiredMessage}
                  />
                </div>
              )}
            </aside>

            <form
              action={placeStorefrontOrderAction}
              className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]"
            >
              <input type="hidden" name="cartId" value={cart.cartId} />
              <input type="hidden" name="fullName" value={draft.fullName} />
              <input type="hidden" name="company" value={draft.company} />
              <input type="hidden" name="street1" value={draft.street1} />
              <input type="hidden" name="street2" value={draft.street2} />
              <input type="hidden" name="postalCode" value={draft.postalCode} />
              <input type="hidden" name="city" value={draft.city} />
              <input type="hidden" name="state" value={draft.state} />
              <input type="hidden" name="countryCode" value={draft.countryCode} />
              <input type="hidden" name="phoneE164" value={draft.phoneE164} />
              <input
                type="hidden"
                name="selectedShippingMethodId"
                value={intent?.selectedShippingMethodId ?? draft.selectedShippingMethodId}
              />
              <input
                type="hidden"
                name="shippingTotalMinor"
                value={String(intent?.selectedShippingTotalMinor ?? 0)}
              />
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.placeOrderEyebrow}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.placeOrderDescription}
              </p>
              <button
                type="submit"
                disabled={!canPlaceOrder}
                className="mt-5 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)] disabled:cursor-not-allowed disabled:opacity-50"
              >
                {copy.placeOrderButton}
              </button>
            </form>
          </div>
        </div>
      </div>
    </section>
  );
}
