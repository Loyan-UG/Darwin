import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { removeCartItemAction, updateCartQuantityAction } from "@/features/cart/actions";
import type { CartViewModel } from "@/features/cart/server/get-cart-view-model";
import { formatMoney } from "@/lib/formatting";

type CartPageProps = {
  model: CartViewModel;
  cartStatus?: string;
  cartError?: string;
};

function getStatusMessage(status?: string) {
  switch (status) {
    case "added":
      return "The item was added to the cart.";
    case "updated":
      return "Cart quantity was updated.";
    case "removed":
      return "The item was removed from the cart.";
    default:
      return undefined;
  }
}

export function CartPage({ model, cartStatus, cartError }: CartPageProps) {
  const statusMessage = getStatusMessage(cartStatus);
  const cart = model.cart;

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            Public cart
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            Storefront cart is now a real public commerce slice
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            This cart runs against the public `Darwin.WebApi` cart endpoints and keeps anonymous storefront identity stable through a dedicated web-owned cookie.
          </p>
        </div>

        {statusMessage && (
          <StatusBanner
            title="Cart updated"
            message={statusMessage}
          />
        )}

        {cartError && (
          <StatusBanner
            tone="warning"
            title="Cart action failed"
            message={cartError}
          />
        )}

        {model.status !== "ok" && model.status !== "empty" && (
          <StatusBanner
            tone="warning"
            title="Cart is running in degraded mode."
            message={model.message ?? `Cart fetch returned status "${model.status}".`}
          />
        )}

        {!cart || cart.items.length === 0 ? (
          <div className="rounded-[2rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-text-muted)]">
              Empty cart
            </p>
            <h2 className="mt-4 font-[family-name:var(--font-display)] text-3xl text-[var(--color-text-primary)]">
              No items are in the storefront cart yet.
            </h2>
            <p className="mt-4 text-base leading-8 text-[var(--color-text-secondary)]">
              Add a product from a detail page to create an anonymous storefront cart and validate the public cart contracts end to end.
            </p>
            <div className="mt-8">
              <Link
                href="/catalog"
                className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
              >
                Browse catalog
              </Link>
            </div>
          </div>
        ) : (
          <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_340px]">
            <div className="flex flex-col gap-5">
              {cart.items.map((item) => (
                <article
                  key={`${item.variantId}:${item.selectedAddOnValueIdsJson}`}
                  className="grid gap-5 rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-5 shadow-[var(--shadow-panel)] md:grid-cols-[160px_minmax(0,1fr)]"
                >
                  <div className="flex min-h-40 items-center justify-center rounded-[1.5rem] bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-4">
                    {item.display?.imageUrl ? (
                      // eslint-disable-next-line @next/next/no-img-element
                      <img
                        src={item.display.imageUrl}
                        alt={item.display.imageAlt || item.display.name}
                        className="max-h-28 w-auto object-contain"
                      />
                    ) : (
                      <span className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-text-muted)]">
                        No image
                      </span>
                    )}
                  </div>
                  <div className="flex flex-col gap-4">
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                        Cart item
                      </p>
                      <h2 className="mt-2 text-2xl font-semibold text-[var(--color-text-primary)]">
                        {item.display?.href ? (
                          <Link href={item.display.href} className="transition hover:text-[var(--color-brand)]">
                            {item.display.name}
                          </Link>
                        ) : (
                          item.display?.name ?? "Storefront variant"
                        )}
                      </h2>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        SKU: {item.display?.sku ?? "Unavailable"} | Variant ID: {item.variantId}
                      </p>
                    </div>

                    <div className="flex flex-wrap items-end justify-between gap-4">
                      <div className="text-sm leading-7 text-[var(--color-text-secondary)]">
                          <p>Line total: {formatMoney(item.lineGrossMinor, cart.currency)}</p>
                          <p>VAT: {formatMoney(item.lineVatMinor, cart.currency)}</p>
                      </div>

                      <div className="flex flex-wrap items-center gap-3">
                        <form action={updateCartQuantityAction} className="flex items-center gap-2">
                          <input type="hidden" name="cartId" value={cart.cartId} />
                          <input type="hidden" name="variantId" value={item.variantId} />
                          <input
                            type="hidden"
                            name="selectedAddOnValueIdsJson"
                            value={item.selectedAddOnValueIdsJson}
                          />
                          <input
                            type="number"
                            name="quantity"
                            min={0}
                            defaultValue={item.quantity}
                            className="w-20 rounded-full border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)]"
                          />
                          <button
                            type="submit"
                            className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                          >
                            Update
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
                            className="rounded-full border border-[rgba(217,111,50,0.2)] px-4 py-2 text-sm font-semibold text-[var(--color-accent)] transition hover:bg-[rgba(217,111,50,0.08)]"
                          >
                            Remove
                          </button>
                        </form>
                      </div>
                    </div>
                  </div>
                </article>
              ))}
            </div>

            <aside className="h-fit rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                Summary
              </p>
              <div className="mt-5 space-y-3 text-sm text-[var(--color-text-secondary)]">
                <div className="flex items-center justify-between">
                  <span>Items</span>
                  <span>{cart.items.length}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>Subtotal net</span>
                  <span>{formatMoney(cart.subtotalNetMinor, cart.currency)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>VAT total</span>
                  <span>{formatMoney(cart.vatTotalMinor, cart.currency)}</span>
                </div>
                <div className="flex items-center justify-between border-t border-[var(--color-border-soft)] pt-3 text-base font-semibold text-[var(--color-text-primary)]">
                  <span>Grand total</span>
                  <span>{formatMoney(cart.grandTotalGrossMinor, cart.currency)}</span>
                </div>
              </div>
              <div className="mt-6 flex flex-col gap-3">
                <Link
                  href="/checkout"
                  className="inline-flex items-center justify-center rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  Start checkout
                </Link>
                <Link
                  href="/catalog"
                  className="inline-flex items-center justify-center rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  Continue shopping
                </Link>
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  Public checkout now sits on top of this cart using the live intent, order-placement, and confirmation contracts from `Darwin.WebApi`.
                </div>
              </div>
            </aside>
          </div>
        )}
      </div>
    </section>
  );
}
