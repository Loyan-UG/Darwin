import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { CartViewModel } from "@/features/cart/server/get-cart-view-model";
import { placeStorefrontOrderAction } from "@/features/checkout/actions";
import { isCheckoutAddressComplete } from "@/features/checkout/helpers";
import type { CheckoutDraft, PublicCheckoutIntent } from "@/features/checkout/types";
import type {
  MemberAddress,
} from "@/features/member-portal/types";
import {
  getCommerceResource,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { formatMoney } from "@/lib/formatting";
import { localizeHref, sanitizeAppPath } from "@/lib/locale-routing";
import { toWebApiUrl } from "@/lib/webapi-url";

type CheckoutPageProps = {
  culture: string;
  model: CartViewModel;
  draft: CheckoutDraft;
  intent: PublicCheckoutIntent | null;
  intentStatus: string;
  intentMessage?: string;
  checkoutError?: string;
  memberAddresses: MemberAddress[];
  profilePrefillActive: boolean;
  selectedMemberAddressId?: string;
  hasMemberSession: boolean;
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
  memberAddresses,
  profilePrefillActive,
  selectedMemberAddressId,
  hasMemberSession,
}: CheckoutPageProps) {
  const copy = getCommerceResource(culture);
  const cart = model.cart;
  const resolvedCheckoutError = resolveLocalizedQueryMessage(checkoutError, copy);
  const resolvedCartMessage = resolveLocalizedQueryMessage(model.message, copy);
  const resolvedIntentMessage = resolveLocalizedQueryMessage(intentMessage, copy);

  if (!cart) {
    return (
      <section className="mx-auto flex w-full max-w-[1180px] flex-1 px-5 py-10 sm:px-6 lg:px-8">
        <div className="w-full rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)]">
          <StatusBanner
            tone="warning"
            title={copy.checkoutUnavailableTitle}
            message={resolvedCartMessage ?? copy.checkoutUnavailableMessage}
          />
          <div className="mt-6 flex flex-wrap gap-3">
            <Link
              href={localizeHref("/cart", culture)}
              className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)]"
            >
              {copy.backToCart}
            </Link>
            <Link
              href={localizeHref("/catalog", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold"
            >
              {copy.continueShopping}
            </Link>
          </div>
        </div>
      </section>
    );
  }

  const addressComplete = isCheckoutAddressComplete(draft);
  const requiresShipping = intent?.requiresShipping ?? true;
  const hasSelectedShipping =
    !requiresShipping ||
    !intent ||
    !intent.shippingOptions.length ||
    Boolean(intent.selectedShippingMethodId || draft.selectedShippingMethodId);
  const canPlaceOrder = Boolean(cart && intent && hasSelectedShipping);
  const projectedCheckoutTotalMinor = getFinalTotalMinor(
    intent,
    cart.grandTotalGrossMinor,
  );

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
          <Link href={localizeHref("/cart", culture)} className="transition hover:text-[var(--color-brand)]">
            {copy.commerceBreadcrumbCart}
          </Link>
          <span>/</span>
          <span className="font-medium text-[var(--color-text-primary)]">
            {copy.commerceBreadcrumbCheckout}
          </span>
        </nav>

        <header className="rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_48%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.publicCheckoutEyebrow}
          </p>
          <div className="mt-4 flex flex-wrap items-end justify-between gap-6">
            <div>
              <h1 className="font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
                {copy.checkoutHeroTitle}
              </h1>
              <p className="mt-4 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)]">
                {copy.checkoutHeroDescription}
              </p>
            </div>
            <div className="rounded-[1rem] bg-white/80 px-5 py-4 text-right shadow-[0_20px_40px_-30px_rgba(58,92,35,0.45)]">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.estimatedGrandTotalLabel}
              </p>
              <p className="mt-2 text-2xl font-semibold text-[var(--color-text-primary)]">
                {formatMoney(projectedCheckoutTotalMinor, cart.currency, culture)}
              </p>
            </div>
          </div>
        </header>

        {resolvedCheckoutError ? (
          <StatusBanner
            tone="warning"
            title={copy.checkoutActionFailedTitle}
            message={resolvedCheckoutError}
          />
        ) : null}

        {!addressComplete ? (
          <StatusBanner title={copy.addressIncompleteTitle} message={copy.addressIncompleteMessage} />
        ) : null}

        {intentStatus !== "idle" && intentStatus !== "ok" ? (
          <StatusBanner
            tone="warning"
            title={copy.checkoutSummaryUnavailableTitle}
            message={resolvedIntentMessage ?? copy.checkoutUnavailableMessage}
          />
        ) : null}

        {hasMemberSession && memberAddresses.length > 0 ? (
          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                  {copy.savedAddressEyebrow}
                </p>
                <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {profilePrefillActive ? copy.savedProfilePrefillActiveMessage : copy.savedAddressDescription}
                </p>
              </div>
              <Link
                href={localizeHref("/account/addresses", culture)}
                className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.savedAddressManageCta}
              </Link>
            </div>
            <div className="mt-5 grid gap-3 md:grid-cols-2">
              {memberAddresses.slice(0, 4).map((address) => (
                <form
                  key={address.id}
                  action={localizeHref("/checkout", culture)}
                  className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                >
                  <input type="hidden" name="memberAddressId" value={address.id} />
                  <input type="hidden" name="fullName" value={address.fullName} />
                  <input type="hidden" name="company" value={address.company ?? ""} />
                  <input type="hidden" name="street1" value={address.street1} />
                  <input type="hidden" name="street2" value={address.street2 ?? ""} />
                  <input type="hidden" name="postalCode" value={address.postalCode} />
                  <input type="hidden" name="city" value={address.city} />
                  <input type="hidden" name="state" value={address.state ?? ""} />
                  <input type="hidden" name="countryCode" value={address.countryCode} />
                  <input type="hidden" name="phoneE164" value={address.phoneE164 ?? ""} />
                  <p className="font-semibold text-[var(--color-text-primary)]">{address.fullName}</p>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {address.street1}, {address.postalCode} {address.city}
                  </p>
                  <button
                    type="submit"
                    className="mt-4 rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold transition hover:bg-[var(--color-surface-panel)]"
                  >
                    {selectedMemberAddressId === address.id
                      ? copy.savedAddressSelectedLabel
                      : copy.savedAddressUseCta}
                  </button>
                </form>
              ))}
            </div>
          </section>
        ) : null}

        <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_360px]">
          <form
            action={localizeHref("/checkout", culture)}
            className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)] sm:px-8"
          >
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.deliveryAddressTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.deliveryAddressDescription}
            </p>

            <div className="mt-6 grid gap-4 sm:grid-cols-2">
              <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
                {copy.fullNameLabel}
                <input name="fullName" defaultValue={draft.fullName} required autoComplete="name" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
              </label>
              <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
                {copy.companyLabel}
                <input name="company" defaultValue={draft.company} autoComplete="organization" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
              </label>
              <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
                {copy.street1Label}
                <input name="street1" defaultValue={draft.street1} required autoComplete="address-line1" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
              </label>
              <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
                {copy.street2Label}
                <input name="street2" defaultValue={draft.street2} autoComplete="address-line2" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
              </label>
              <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                {copy.postalCodeLabel}
                <input name="postalCode" defaultValue={draft.postalCode} required autoComplete="postal-code" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
              </label>
              <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                {copy.cityLabel}
                <input name="city" defaultValue={draft.city} required autoComplete="address-level2" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
              </label>
              <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                {copy.stateLabel}
                <input name="state" defaultValue={draft.state} autoComplete="address-level1" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
              </label>
              <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                {copy.countryCodeLabel}
                <input name="countryCode" defaultValue={draft.countryCode} required autoComplete="country" maxLength={2} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal uppercase outline-none" />
              </label>
              <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
                {copy.phoneLabel}
                <input name="phoneE164" defaultValue={draft.phoneE164} autoComplete="tel" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
              </label>
            </div>

            {intent?.requiresShipping && intent.shippingOptions.length > 0 ? (
              <div className="mt-6 grid gap-3 rounded-[1rem] bg-[var(--color-surface-panel-strong)] p-5">
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
            ) : null}

            <div className="mt-6 flex flex-wrap gap-3">
              <button
                type="submit"
                className="rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
              >
                {copy.refreshCheckoutSummary}
              </button>
              <Link
                href={localizeHref("/cart", culture)}
                className="rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.backToCart}
              </Link>
            </div>
          </form>

          <aside className="flex h-fit flex-col gap-5">
            <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <h2 className="text-xl font-semibold text-[var(--color-text-primary)]">
                {copy.checkoutSummaryTitle}
              </h2>
              <div className="mt-5 space-y-3 text-sm text-[var(--color-text-secondary)]">
                <div className="flex items-center justify-between">
                  <span>{copy.cartLinesLabel}</span>
                  <span>{cart.items.length}</span>
                </div>
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
                  <span>{formatMoney(projectedCheckoutTotalMinor, cart.currency, culture)}</span>
                </div>
              </div>

              {intent?.requiresShipping && !hasSelectedShipping ? (
                <div className="mt-5">
                  <StatusBanner
                    tone="warning"
                    title={copy.shippingSelectionMissingTitle}
                    message={copy.shippingSelectionMissingMessage}
                  />
                </div>
              ) : null}

              {!intent ? (
                <div className="mt-5">
                  <StatusBanner
                    title={copy.checkoutSummaryPendingTitle}
                    message={copy.checkoutSummaryPendingMessage}
                  />
                </div>
              ) : null}
            </div>

            <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">
                {copy.checkoutLinesTitle}
              </h2>
              <div className="mt-5 flex flex-col gap-4">
                {cart.items.map((item) => {
                  const itemImageUrl = toWebApiUrl(item.display?.imageUrl ?? "");
                  const itemImageAlt =
                    item.display?.imageAlt || item.display?.name || copy.storefrontVariantFallback;
                  const itemProductHref = sanitizeAppPath(item.display?.href, "/catalog");

                  return (
                    <article
                      key={`${item.variantId}:${item.selectedAddOnValueIdsJson}`}
                      className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                    >
                      <div className="flex items-start gap-4">
                        <div className="flex h-16 w-16 shrink-0 items-center justify-center rounded-[1rem] bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-3">
                          {itemImageUrl ? (
                            // eslint-disable-next-line @next/next/no-img-element
                            <img src={itemImageUrl} alt={itemImageAlt} className="max-h-10 w-auto object-contain" />
                          ) : (
                            <span className="text-[10px] font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                              {copy.noImage}
                            </span>
                          )}
                        </div>
                        <div className="min-w-0 flex-1">
                          <Link
                            href={localizeHref(itemProductHref, culture)}
                            className="text-sm font-semibold text-[var(--color-text-primary)] transition hover:text-[var(--color-brand)]"
                          >
                            {item.display?.name ?? copy.storefrontVariantFallback}
                          </Link>
                          <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
                            {copy.cartQuantityAriaLabel}: {item.quantity}
                          </p>
                        </div>
                        <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                          {formatMoney(item.lineGrossMinor, cart.currency, culture)}
                        </p>
                      </div>
                    </article>
                  );
                })}
              </div>
            </div>

            <form
              action={placeStorefrontOrderAction}
              className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]"
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
              <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.placeOrderDescription}
              </p>
              <button
                type="submit"
                disabled={!canPlaceOrder}
                className="mt-5 w-full rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)] disabled:cursor-not-allowed disabled:opacity-50"
              >
                {copy.placeOrderButton}
              </button>
            </form>
          </aside>
        </div>
      </div>
    </section>
  );
}
