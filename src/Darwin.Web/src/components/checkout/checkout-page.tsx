import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { placeStorefrontOrderAction } from "@/features/checkout/actions";
import { isCheckoutAddressComplete } from "@/features/checkout/helpers";
import type { CheckoutDraft, PublicCheckoutIntent } from "@/features/checkout/types";
import type { CartViewModel } from "@/features/cart/server/get-cart-view-model";
import { formatMoney } from "@/lib/formatting";

type CheckoutPageProps = {
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
  model,
  draft,
  intent,
  intentStatus,
  intentMessage,
  checkoutError,
}: CheckoutPageProps) {
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
            title="Checkout is unavailable without a cart."
            message={model.message ?? "Add at least one published storefront product before starting the public checkout flow."}
          />
          <div>
            <Link
              href="/catalog"
              className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              Browse catalog
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
            Public checkout
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            Checkout now runs on the live storefront intent and order contracts
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            Enter the delivery address, review the rated shipping options, and place an order through the public `Darwin.WebApi` checkout flow.
          </p>
        </div>

        {checkoutError && (
          <StatusBanner
            tone="warning"
            title="Checkout action failed"
            message={checkoutError}
          />
        )}

        {!addressComplete && (
          <StatusBanner
            title="Address details are still incomplete."
            message="Fill the required fields and refresh the checkout preview to calculate authoritative shipping and totals."
          />
        )}

        {intentStatus !== "idle" && intentStatus !== "ok" && (
          <StatusBanner
            tone="warning"
            title="Checkout preview is degraded."
            message={intentMessage ?? `Checkout intent returned status "${intentStatus}".`}
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
                  Billing and shipping
                </p>
                <h2 className="mt-3 text-3xl font-[family-name:var(--font-display)] text-[var(--color-text-primary)]">
                  Delivery address
                </h2>
                <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                  This first slice keeps billing and shipping aligned to one address so the storefront checkout can stay small and contract-first.
                </p>
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  Full name*
                  <input name="fullName" defaultValue={draft.fullName} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  Company
                  <input name="company" defaultValue={draft.company} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
                  Street line 1*
                  <input name="street1" defaultValue={draft.street1} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
                  Street line 2
                  <input name="street2" defaultValue={draft.street2} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  Postal code*
                  <input name="postalCode" defaultValue={draft.postalCode} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  City*
                  <input name="city" defaultValue={draft.city} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  State / region
                  <input name="state" defaultValue={draft.state} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  Country code*
                  <input name="countryCode" defaultValue={draft.countryCode} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal uppercase text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
                  Phone (E.164)
                  <input name="phoneE164" defaultValue={draft.phoneE164} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
              </div>

              {intent?.requiresShipping && intent.shippingOptions.length > 0 && (
                <div className="grid gap-3 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] p-5">
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                      Shipping options
                    </p>
                    <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      Select one option and refresh the preview before placing the order.
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
                          {formatMoney(option.priceMinor, option.currency)}
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
                  Refresh checkout preview
                </button>
                <Link
                  href="/cart"
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  Back to cart
                </Link>
              </div>
            </div>
          </form>

          <div className="flex flex-col gap-5">
            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                Checkout summary
              </p>
              <div className="mt-5 space-y-3 text-sm text-[var(--color-text-secondary)]">
                <div className="flex items-center justify-between">
                  <span>Cart lines</span>
                  <span>{cart.items.length}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>Subtotal net</span>
                  <span>{formatMoney(intent?.subtotalNetMinor ?? cart.subtotalNetMinor, cart.currency)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>VAT total</span>
                  <span>{formatMoney(intent?.vatTotalMinor ?? cart.vatTotalMinor, cart.currency)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>Shipping</span>
                  <span>{formatMoney(intent?.selectedShippingTotalMinor ?? 0, cart.currency)}</span>
                </div>
                <div className="flex items-center justify-between border-t border-[var(--color-border-soft)] pt-3 text-base font-semibold text-[var(--color-text-primary)]">
                  <span>Estimated grand total</span>
                  <span>{formatMoney(getFinalTotalMinor(intent, cart.grandTotalGrossMinor), cart.currency)}</span>
                </div>
              </div>

              {intent?.requiresShipping && !hasSelectedShipping && (
                <div className="mt-5">
                  <StatusBanner
                    tone="warning"
                    title="Shipping selection is still missing."
                    message="Choose a shipping option in the preview form and refresh checkout before placing the order."
                  />
                </div>
              )}

              {!intent && (
                <div className="mt-5">
                  <StatusBanner
                    title="No authoritative checkout preview yet."
                    message="The order button becomes available after the address is complete and the public checkout intent succeeds."
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
                Place order
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                This creates the order through the public checkout endpoint and then moves the shopper into the confirmation and payment handoff flow.
              </p>
              <button
                type="submit"
                disabled={!canPlaceOrder}
                className="mt-5 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)] disabled:cursor-not-allowed disabled:opacity-50"
              >
                Place order
              </button>
            </form>
          </div>
        </div>
      </div>
    </section>
  );
}
