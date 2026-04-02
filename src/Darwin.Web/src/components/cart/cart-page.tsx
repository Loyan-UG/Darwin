import Link from "next/link";
import { CommerceAuthHandoff } from "@/components/checkout/commerce-auth-handoff";
import { CommerceContinuationRail } from "@/components/checkout/commerce-continuation-rail";
import { StatusBanner } from "@/components/feedback/status-banner";
import {
  applyCartCouponAction,
  removeCartItemAction,
  updateCartQuantityAction,
} from "@/features/cart/actions";
import type { PublicProductSummary } from "@/features/catalog/types";
import type { CartViewModel } from "@/features/cart/server/get-cart-view-model";
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
  formatResource,
  getCommerceResource,
  resolveLocalizedQueryMessage,
} from "@/localization";
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

function getStatusMessage(status?: string) {
  switch (status) {
    case "added":
      return "cartItemAdded";
    case "updated":
      return "cartQuantityUpdated";
    case "removed":
      return "cartItemRemoved";
    case "coupon-applied":
      return "cartCouponApplied";
    case "coupon-cleared":
      return "cartCouponCleared";
    default:
      return undefined;
  }
}

export function CartPage({
  culture,
  model,
  memberAddresses,
  memberAddressesStatus,
  memberProfile,
  memberProfileStatus,
  memberPreferences,
  memberPreferencesStatus,
  hasMemberSession,
  cartStatus,
  cartError,
  followUpProducts = [],
}: CartPageProps) {
  const copy = getCommerceResource(culture);
  const statusMessageKey = getStatusMessage(cartStatus);
  const statusMessage = statusMessageKey
    ? copy[statusMessageKey as keyof typeof copy]
    : undefined;
  const resolvedCartError = resolveLocalizedQueryMessage(cartError, copy);
  const resolvedModelMessage = resolveLocalizedQueryMessage(model.message, copy);
  const cart = model.cart;
  const preferredCheckoutAddress =
    memberAddresses.find((address) => address.isDefaultShipping) ??
    memberAddresses.find((address) => address.isDefaultBilling) ??
    memberAddresses[0] ??
    null;
  const emailChannelReady = Boolean(
    memberProfile?.email && memberPreferences?.allowEmailMarketing,
  );
  const smsChannelReady = Boolean(
    memberProfile?.phoneE164 &&
      memberProfile.phoneNumberConfirmed &&
      memberPreferences?.allowSmsMarketing,
  );
  const whatsAppChannelReady = Boolean(
    memberProfile?.phoneE164 &&
      memberProfile.phoneNumberConfirmed &&
      memberPreferences?.allowWhatsAppMarketing,
  );
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
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
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

        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.cartHeroEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.cartHeroTitle}
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {copy.cartHeroDescription}
          </p>
        </div>

        {statusMessage && (
          <StatusBanner
            title={copy.cartUpdatedTitle}
            message={statusMessage}
          />
        )}

        {resolvedCartError && (
          <StatusBanner
            tone="warning"
            title={copy.cartActionFailedTitle}
            message={resolvedCartError}
          />
        )}

        {model.status !== "ok" && model.status !== "empty" && (
          <StatusBanner
            tone="warning"
            title={copy.cartDegradedTitle}
            message={resolvedModelMessage ?? formatResource(copy.cartDegradedMessage, {
              status: model.status,
            })}
          />
        )}

        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
            {copy.cartRouteSummaryTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.cartRouteSummaryMessage, {
              status: model.status,
              itemCount: cart?.items.length ?? 0,
              followUpCount: followUpProducts.length,
            })}
          </p>
        </div>

        {cart && cart.items.length > 0 && (
          <div className="grid gap-5 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)]">
            <section className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.cartOpportunityTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {followUpProducts.length > 0
                  ? formatResource(copy.cartOpportunityMessage, {
                      count: followUpProducts.length,
                    })
                  : copy.cartOpportunityFallback}
              </p>
              {followUpProducts.find(
                (product) =>
                  typeof product.compareAtPriceMinor === "number" &&
                  product.compareAtPriceMinor > product.priceMinor,
              ) ? (
                (() => {
                  const bestOffer =
                    followUpProducts
                      .filter(
                        (product) =>
                          typeof product.compareAtPriceMinor === "number" &&
                          product.compareAtPriceMinor > product.priceMinor,
                      )
                      .map((product) => ({
                        product,
                        savingsPercent: Math.round(
                          ((product.compareAtPriceMinor! - product.priceMinor) /
                            product.compareAtPriceMinor!) *
                            100,
                        ),
                      }))
                      .sort((left, right) => right.savingsPercent - left.savingsPercent)[0] ?? null;

                  if (!bestOffer) {
                    return null;
                  }

                  return (
                    <div className="mt-5 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
                      <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                        {bestOffer.product.name}
                      </p>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {formatResource(copy.cartOpportunityOfferMessage, {
                          savingsPercent: bestOffer.savingsPercent,
                        })}
                      </p>
                      <div className="mt-4 flex flex-wrap items-end gap-3">
                        <p className="text-lg font-semibold text-[var(--color-text-primary)]">
                          {formatMoney(
                            bestOffer.product.priceMinor,
                            bestOffer.product.currency,
                            culture,
                          )}
                        </p>
                        <p className="text-sm text-[var(--color-text-muted)] line-through">
                          {formatMoney(
                            bestOffer.product.compareAtPriceMinor!,
                            bestOffer.product.currency,
                            culture,
                          )}
                        </p>
                      </div>
                    </div>
                  );
                })()
              ) : null}
            </section>

            <section className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.cartReadinessTitle}
              </p>
              <div className="mt-4 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.cartReadinessLinesLabel}
                  </p>
                  <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                    {formatResource(copy.cartReadinessLinesValue, {
                      count: cart.items.length,
                    })}
                  </p>
                </div>
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.cartReadinessCouponLabel}
                  </p>
                  <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                    {cart.couponCode
                      ? copy.cartReadinessCouponApplied
                      : copy.cartReadinessCouponNone}
                  </p>
                </div>
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.cartReadinessCheckoutLabel}
                  </p>
                  <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                    {hasMemberSession && preferredCheckoutAddress
                      ? copy.cartReadinessCheckoutPrepared
                      : copy.cartReadinessCheckoutOpen}
                  </p>
                </div>
              </div>
            </section>
          </div>
        )}

        {!cart || cart.items.length === 0 ? (
          <div className="rounded-[2rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-text-muted)]">
              {copy.emptyCartEyebrow}
            </p>
            <h2 className="mt-4 font-[family-name:var(--font-display)] text-3xl text-[var(--color-text-primary)]">
              {copy.emptyCartTitle}
            </h2>
            <p className="mt-4 text-base leading-8 text-[var(--color-text-secondary)]">
              {copy.emptyCartDescription}
            </p>
            <div className="mt-8 text-left">
              <CommerceContinuationRail
                culture={culture}
                includeCms
                includeAccount={false}
              />
            </div>
          </div>
        ) : (
          <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_340px]">
            <div className="flex flex-col gap-5">
              {cart.items.map((item) => (
                (() => {
                  const itemImageUrl = toWebApiUrl(item.display?.imageUrl ?? "");
                  const itemImageAlt =
                    item.display?.imageAlt || item.display?.name || copy.storefrontVariantFallback;
                  const itemProductHref = sanitizeAppPath(
                    item.display?.href,
                    "/catalog",
                  );
                  return (
                <article
                  key={`${item.variantId}:${item.selectedAddOnValueIdsJson}`}
                  className="grid gap-5 rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-5 shadow-[var(--shadow-panel)] md:grid-cols-[160px_minmax(0,1fr)]"
                >
                  <div className="flex min-h-40 items-center justify-center rounded-[1.5rem] bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-4">
                    {itemImageUrl ? (
                      // eslint-disable-next-line @next/next/no-img-element
                      <img
                        src={itemImageUrl}
                        alt={itemImageAlt}
                        className="max-h-28 w-auto object-contain"
                      />
                    ) : (
                        <span className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-text-muted)]">
                          {copy.noImage}
                        </span>
                      )}
                  </div>
                  <div className="flex flex-col gap-4">
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                        {copy.cartItemEyebrow}
                      </p>
                      <h2 className="mt-2 text-2xl font-semibold text-[var(--color-text-primary)]">
                        {item.display?.href ? (
                          <Link href={localizeHref(itemProductHref, culture)} className="transition hover:text-[var(--color-brand)]">
                            {item.display.name}
                          </Link>
                        ) : (
                          item.display?.name ?? copy.storefrontVariantFallback
                        )}
                      </h2>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {formatResource(copy.skuVariantLine, {
                          sku: item.display?.sku ?? copy.unavailable,
                          variantId: item.variantId,
                        })}
                      </p>
                    </div>

                    <div className="flex flex-wrap items-end justify-between gap-4">
                      <div className="text-sm leading-7 text-[var(--color-text-secondary)]">
                          <p>{copy.unitNetLabel} {formatMoney(item.unitPriceNetMinor, cart.currency, culture)}</p>
                          {item.addOnPriceDeltaMinor > 0 ? (
                            <p>{copy.addOnsLabel} {formatMoney(item.addOnPriceDeltaMinor, cart.currency, culture)}</p>
                          ) : null}
                          <p>{copy.vatRateLabel} {(item.vatRate * 100).toFixed(0)}%</p>
                          <p>{copy.lineNetLabel} {formatMoney(item.lineNetMinor, cart.currency, culture)}</p>
                          <p>{copy.lineTotalLabel} {formatMoney(item.lineGrossMinor, cart.currency, culture)}</p>
                          <p>{copy.vatLabel} {formatMoney(item.lineVatMinor, cart.currency, culture)}</p>
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
                            step={1}
                            inputMode="numeric"
                            defaultValue={item.quantity}
                            aria-label={copy.cartQuantityAriaLabel}
                            className="w-20 rounded-full border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)]"
                          />
                          <button
                            type="submit"
                            className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
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
                            className="rounded-full border border-[rgba(217,111,50,0.2)] px-4 py-2 text-sm font-semibold text-[var(--color-accent)] transition hover:bg-[rgba(217,111,50,0.08)]"
                          >
                            {copy.remove}
                          </button>
                        </form>
                      </div>
                    </div>
                  </div>
                </article>
                  );
                })()
              ))}
            </div>

            <aside className="h-fit rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.summaryTitle}
              </p>
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
                    autoCapitalize="characters"
                    autoComplete="off"
                    maxLength={64}
                    className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none"
                  />
                </label>
                <div className="flex flex-wrap gap-3">
                  <button
                    type="submit"
                    className="inline-flex items-center justify-center rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.applyOrClearCoupon}
                  </button>
                </div>
              </form>
              {hasMemberSession && (
                <>
                  <div className="mt-6 rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
                      {copy.cartMemberCheckoutTitle}
                    </p>
                    <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {formatResource(copy.cartMemberCheckoutMessage, {
                        status: memberAddressesStatus,
                        count: memberAddresses.length,
                      })}
                    </p>
                    {preferredCheckoutAddress ? (
                      <div className="mt-4 rounded-[1.25rem] bg-[var(--color-surface-panel)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                        <p className="font-semibold text-[var(--color-text-primary)]">
                          {preferredCheckoutAddress.fullName}
                        </p>
                        {preferredCheckoutAddress.company ? (
                          <p>{preferredCheckoutAddress.company}</p>
                        ) : null}
                        <p>{preferredCheckoutAddress.street1}</p>
                        {preferredCheckoutAddress.street2 ? (
                          <p>{preferredCheckoutAddress.street2}</p>
                        ) : null}
                        <p>
                          {preferredCheckoutAddress.postalCode} {preferredCheckoutAddress.city}
                        </p>
                        <p>{preferredCheckoutAddress.countryCode}</p>
                        <div className="mt-3 flex flex-wrap gap-2 text-xs font-semibold uppercase tracking-[0.16em] text-[var(--color-text-primary)]">
                          {preferredCheckoutAddress.isDefaultShipping ? (
                            <span className="rounded-full bg-[var(--color-surface-panel-strong)] px-3 py-1">
                              {copy.cartMemberCheckoutDefaultShippingLabel}
                            </span>
                          ) : null}
                          {preferredCheckoutAddress.isDefaultBilling ? (
                            <span className="rounded-full bg-[var(--color-surface-panel-strong)] px-3 py-1">
                              {copy.cartMemberCheckoutDefaultBillingLabel}
                            </span>
                          ) : null}
                        </div>
                      </div>
                    ) : (
                      <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {copy.cartMemberCheckoutEmptyMessage}
                      </p>
                    )}
                    <div className="mt-4 flex flex-wrap gap-3">
                      <Link
                        href={checkoutHref}
                        className="inline-flex items-center justify-center rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                      >
                        {preferredCheckoutAddress
                          ? copy.cartMemberCheckoutUseSavedAddressCta
                          : copy.cartMemberCheckoutOpenCheckoutCta}
                      </Link>
                      <Link
                        href={localizeHref("/account/addresses", culture)}
                        className="inline-flex items-center justify-center rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                      >
                        {copy.cartMemberCheckoutManageAddressesCta}
                      </Link>
                    </div>
                  </div>

                  <div className="mt-6 rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
                      {copy.cartMemberContextTitle}
                    </p>
                    <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {formatResource(copy.cartMemberContextMessage, {
                        profileStatus: memberProfileStatus,
                        preferencesStatus: memberPreferencesStatus,
                        addressesStatus: memberAddressesStatus,
                      })}
                    </p>
                    <div className="mt-4 grid gap-3">
                      <div className="rounded-[1.25rem] bg-[var(--color-surface-panel)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                        <p className="font-semibold text-[var(--color-text-primary)]">
                          {copy.cartMemberContextIdentityLabel}
                        </p>
                        <p className="mt-2">
                          {memberProfile?.firstName || memberProfile?.lastName
                            ? `${memberProfile?.firstName ?? ""} ${memberProfile?.lastName ?? ""}`.trim()
                            : copy.unavailable}
                        </p>
                        <p>{memberProfile?.email ?? copy.unavailable}</p>
                      </div>
                      <div className="rounded-[1.25rem] bg-[var(--color-surface-panel)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                        <p className="font-semibold text-[var(--color-text-primary)]">
                          {copy.cartMemberContextPhoneLabel}
                        </p>
                        <p className="mt-2">
                          {memberProfile?.phoneE164 ?? copy.unavailable}
                        </p>
                        <p>
                          {memberProfile?.phoneNumberConfirmed
                            ? copy.cartMemberContextPhoneVerified
                            : copy.cartMemberContextPhonePending}
                        </p>
                      </div>
                      <div className="rounded-[1.25rem] bg-[var(--color-surface-panel)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                        <p className="font-semibold text-[var(--color-text-primary)]">
                          {copy.cartMemberContextChannelsLabel}
                        </p>
                        <p className="mt-2">
                          {formatResource(copy.cartMemberContextEmailValue, {
                            value: emailChannelReady ? copy.readyYes : copy.readyNo,
                          })}
                        </p>
                        <p>
                          {formatResource(copy.cartMemberContextSmsValue, {
                            value: smsChannelReady ? copy.readyYes : copy.readyNo,
                          })}
                        </p>
                        <p>
                          {formatResource(copy.cartMemberContextWhatsAppValue, {
                            value: whatsAppChannelReady ? copy.readyYes : copy.readyNo,
                          })}
                        </p>
                      </div>
                    </div>
                    <div className="mt-4 flex flex-wrap gap-3">
                      <Link
                        href={localizeHref("/account/profile", culture)}
                        className="inline-flex items-center justify-center rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                      >
                        {copy.cartMemberContextProfileCta}
                      </Link>
                      <Link
                        href={localizeHref("/account/preferences", culture)}
                        className="inline-flex items-center justify-center rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                      >
                        {copy.cartMemberContextPreferencesCta}
                      </Link>
                    </div>
                  </div>
                </>
              )}
              {!hasMemberSession && (
                <div className="mt-6">
                  <CommerceAuthHandoff
                    culture={culture}
                    cart={cart}
                    returnPath="/checkout"
                    routeKey="cart"
                  />
                </div>
              )}
              <div className="mt-6 flex flex-col gap-3">
                <Link
                  href={checkoutHref}
                  className="inline-flex items-center justify-center rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  {copy.startCheckout}
                </Link>
                <Link
                  href={localizeHref("/catalog", culture)}
                  className="inline-flex items-center justify-center rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.continueShopping}
                </Link>
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.checkoutSummaryNote}
                </div>
              </div>
            </aside>
          </div>
        )}

        {cart && cart.items.length > 0 && (
          <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_360px]">
            <section className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.followUpProductsTitle}
              </p>
              <p className="mt-3 max-w-3xl text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.followUpProductsDescription}
              </p>

              {followUpProducts.length > 0 ? (
                <div className="mt-5 grid gap-4 md:grid-cols-3">
                  {followUpProducts.map((product) => {
                    const productImageUrl = toWebApiUrl(product.primaryImageUrl ?? "");
                    return (
                    <article
                      key={product.id}
                      className="rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-4"
                    >
                      <div className="flex min-h-32 items-center justify-center rounded-[1.25rem] bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-3">
                        {productImageUrl ? (
                          // eslint-disable-next-line @next/next/no-img-element
                          <img
                            src={productImageUrl}
                            alt={product.name}
                            className="max-h-24 w-auto object-contain"
                          />
                        ) : (
                          <span className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-text-muted)]">
                            {copy.noImage}
                          </span>
                        )}
                      </div>
                      <h2 className="mt-4 text-lg font-semibold text-[var(--color-text-primary)]">
                        <Link
                          href={localizeHref(`/catalog/${product.slug}`, culture)}
                          className="transition hover:text-[var(--color-brand)]"
                        >
                          {product.name}
                        </Link>
                      </h2>
                      <p className="mt-2 min-h-14 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {product.shortDescription || copy.followUpProductFallbackDescription}
                      </p>
                      <div className="mt-4 flex items-center justify-between gap-3">
                        <div className="text-sm font-semibold text-[var(--color-text-primary)]">
                          {formatMoney(product.priceMinor, product.currency, culture)}
                        </div>
                        <Link
                          href={localizeHref(`/catalog/${product.slug}`, culture)}
                          className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                        >
                          {copy.followUpProductCta}
                        </Link>
                      </div>
                    </article>
                    );
                  })}
                </div>
              ) : (
                <div className="mt-5 rounded-[1.5rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-4 py-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  <p>{copy.followUpProductsUnavailableMessage}</p>
                  <div className="mt-6 text-left">
                    <CommerceContinuationRail
                      culture={culture}
                      includeCms={false}
                      includeCart={false}
                    />
                  </div>
                </div>
              )}
            </section>

            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.cartNextStepsTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.cartNextStepsDescription}
              </p>
              <div className="mt-5 grid gap-3">
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-brand)]">
                    {copy.cartNextStepReviewLabel}
                  </p>
                  <h2 className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {copy.cartNextStepReviewTitle}
                  </h2>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {copy.cartNextStepReviewMessage}
                  </p>
                </div>
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-brand)]">
                    {copy.cartNextStepCheckoutLabel}
                  </p>
                  <h2 className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {copy.cartNextStepCheckoutTitle}
                  </h2>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {copy.cartNextStepCheckoutMessage}
                  </p>
                </div>
              </div>
            </aside>

            {!hasMemberSession && (
              <CommerceAuthHandoff
                culture={culture}
                cart={cart}
                returnPath="/checkout"
                routeKey="cart"
              />
            )}

            <CommerceContinuationRail culture={culture} />
          </div>
        )}
      </div>
    </section>
  );
}
