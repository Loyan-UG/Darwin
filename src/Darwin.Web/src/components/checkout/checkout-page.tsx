import Link from "next/link";
import { CommerceAuthHandoff } from "@/components/checkout/commerce-auth-handoff";
import { CommerceContinuationRail } from "@/components/checkout/commerce-continuation-rail";
import { CheckoutContentCompositionWindow } from "@/components/checkout/checkout-content-composition-window";
import { CommerceStorefrontWindow } from "@/components/checkout/commerce-storefront-window";
import { StatusBanner } from "@/components/feedback/status-banner";
import { placeStorefrontOrderAction } from "@/features/checkout/actions";
import { isCheckoutAddressComplete } from "@/features/checkout/helpers";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { CheckoutDraft, PublicCheckoutIntent } from "@/features/checkout/types";
import type { CartViewModel } from "@/features/cart/server/get-cart-view-model";
import type { PublicPageSummary } from "@/features/cms/types";
import type {
  MemberAddress,
  MemberCustomerProfile,
  MemberInvoiceSummary,
  MemberPreferences,
} from "@/features/member-portal/types";
import {
  formatResource,
  getCommerceResource,
  resolveApiStatusLabel,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { buildInvoicePath } from "@/lib/entity-paths";
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
  memberAddressesStatus: string;
  memberProfile: MemberCustomerProfile | null;
  memberProfileStatus: string;
  memberPreferences: MemberPreferences | null;
  memberPreferencesStatus: string;
  memberInvoices: MemberInvoiceSummary[];
  memberInvoicesStatus: string;
  profilePrefillActive: boolean;
  selectedMemberAddressId?: string;
  hasMemberSession: boolean;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
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

function localizeCheckoutStatus(status: string | undefined, culture: string) {
  const normalized = (status ?? "").trim().toLowerCase();
  const english = culture.toLowerCase().startsWith("en");

  if (!normalized) {
    return english ? "Unavailable" : "Nicht verfuegbar";
  }

  const labels: Record<string, { de: string; en: string }> = {
    idle: { de: "Bereit", en: "Ready" },
    ok: { de: "Aktuell", en: "Current" },
    "not-found": { de: "Nicht gefunden", en: "Not found" },
    "http-error": { de: "Dienstfehler", en: "Service error" },
    "network-error": { de: "Netzwerkfehler", en: "Network error" },
    unavailable: { de: "Nicht verfuegbar", en: "Unavailable" },
  };

  const label = labels[normalized];
  return label ? (english ? label.en : label.de) : normalized;
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
  memberAddressesStatus,
  memberProfile,
  memberProfileStatus,
  memberPreferences,
  memberPreferencesStatus,
  memberInvoices,
  memberInvoicesStatus,
  profilePrefillActive,
  selectedMemberAddressId,
  hasMemberSession,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
}: CheckoutPageProps) {
  const copy = getCommerceResource(culture);
  const resolvedCheckoutError = resolveLocalizedQueryMessage(checkoutError, copy);
  const resolvedCartMessage = resolveLocalizedQueryMessage(model.message, copy);
  const resolvedIntentMessage = resolveLocalizedQueryMessage(intentMessage, copy);
  const localizedCartStatus = localizeCheckoutStatus(model.status, culture);
  const localizedIntentStatus = localizeCheckoutStatus(intentStatus, culture);
  const memberAddressesStatusLabel =
    resolveApiStatusLabel(memberAddressesStatus, copy) ?? memberAddressesStatus;
  const memberProfileStatusLabel =
    resolveApiStatusLabel(memberProfileStatus, copy) ?? memberProfileStatus;
  const memberPreferencesStatusLabel =
    resolveApiStatusLabel(memberPreferencesStatus, copy) ?? memberPreferencesStatus;
  const memberInvoicesStatusLabel =
    resolveApiStatusLabel(memberInvoicesStatus, copy) ?? memberInvoicesStatus;
  const cart = model.cart;
  const addressComplete = isCheckoutAddressComplete(draft);
  const requiresShipping = intent?.requiresShipping ?? true;
  const hasSelectedShipping =
    !requiresShipping ||
    !intent ||
    !intent.shippingOptions.length ||
    Boolean(intent.selectedShippingMethodId || draft.selectedShippingMethodId);
  const canPlaceOrder = Boolean(cart && intent && hasSelectedShipping);
  const hasSavedAddresses = memberAddresses.length > 0;
  const projectedCheckoutTotalMinor = getFinalTotalMinor(
    intent,
    cart?.grandTotalGrossMinor ?? 0,
  );
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
  const outstandingInvoice = memberInvoices.find((invoice) => invoice.balanceMinor > 0) ?? null;
  const openBillingExposureMinor = outstandingInvoice?.balanceMinor ?? 0;
  const combinedExposureMinor =
    projectedCheckoutTotalMinor + openBillingExposureMinor;
  const readinessItems = [
    {
      label: copy.addressReadyLabel,
      value: addressComplete ? copy.readyYes : copy.readyNo,
    },
    {
      label: copy.shippingReadyLabel,
      value: hasSelectedShipping ? copy.readyYes : copy.readyNo,
    },
    {
      label: copy.intentReadyLabel,
      value: intent ? copy.readyYes : copy.readyNo,
    },
    {
      label: copy.couponStateLabel,
      value: cart?.couponCode ? copy.couponAppliedState : copy.couponMissingState,
    },
  ];
  const invoiceAttentionCount = memberInvoices.filter(
    (invoice) => invoice.balanceMinor > 0,
  ).length;
  const sectionLinks = [
    { id: "checkout-overview", label: copy.checkoutRouteSummaryTitle },
    { id: "checkout-payment-window", label: copy.checkoutPaymentWindowTitle },
    { id: "checkout-form", label: copy.checkoutHeroTitle },
    { id: "checkout-composition", label: copy.checkoutCompositionJourneyTitle },
  ];

  if (!cart) {
    return (
      <section className="mx-auto flex w-full max-w-[1320px] flex-1 px-5 py-10 sm:px-6 lg:px-8">
        <div className="flex w-full flex-col gap-6 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title={copy.checkoutUnavailableTitle}
            message={resolvedCartMessage ?? copy.checkoutUnavailableMessage}
          />
          <div className="mt-2">
            <CommerceContinuationRail
              culture={culture}
              includeCms={false}
              includeCart
            />
          </div>
        </div>
      </section>
    );
  }

  return (
    <section className="mx-auto flex w-full max-w-[1320px] flex-1 px-5 py-10 sm:px-6 lg:px-8">
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
          <Link href={localizeHref("/cart", culture)} className="transition hover:text-[var(--color-brand)]">
            {copy.commerceBreadcrumbCart}
          </Link>
          <span>/</span>
          <span className="font-medium text-[var(--color-text-primary)]">
            {copy.commerceBreadcrumbCheckout}
          </span>
        </nav>

        <div className="overflow-hidden rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_42%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8 sm:py-10">
          <div className="grid gap-8 lg:grid-cols-[minmax(0,1.2fr)_340px] lg:items-end">
            <div>
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
            <div className="grid gap-3 sm:grid-cols-3 lg:grid-cols-1">
              <article className="rounded-[1rem] border border-white/70 bg-white/80 px-5 py-4 shadow-[0_20px_40px_-28px_rgba(58,92,35,0.45)] backdrop-blur">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.cartLinesLabel}
                </p>
                <p className="mt-2 text-xl font-semibold text-[var(--color-text-primary)]">
                  {cart.items.length}
                </p>
              </article>
              <article className="rounded-[1rem] border border-white/70 bg-white/80 px-5 py-4 shadow-[0_20px_40px_-28px_rgba(58,92,35,0.45)] backdrop-blur">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.shippingLabel}
                </p>
                <p className="mt-2 text-xl font-semibold text-[var(--color-text-primary)]">
                  {hasSelectedShipping ? copy.readyYes : copy.readyNo}
                </p>
              </article>
              <article className="rounded-[1rem] border border-white/70 bg-[linear-gradient(135deg,rgba(57,116,47,0.94),rgba(255,145,77,0.92))] px-5 py-4 text-white shadow-[0_20px_40px_-28px_rgba(58,92,35,0.55)]">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/78">
                  {copy.estimatedGrandTotalLabel}
                </p>
                <p className="mt-2 text-xl font-semibold text-white">
                  {formatMoney(projectedCheckoutTotalMinor, cart.currency, culture)}
                </p>
              </article>
            </div>
          </div>
        </div>

        {resolvedCheckoutError && (
          <StatusBanner
            tone="warning"
            title={copy.checkoutActionFailedTitle}
            message={resolvedCheckoutError}
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
            message={resolvedIntentMessage ?? formatResource(copy.previewDegradedMessage, {
              status: localizedIntentStatus,
            })}
          />
        )}

        <div className="grid gap-4 rounded-[1rem] border border-[#dce6cf] bg-white px-6 py-6 shadow-[0_24px_60px_-36px_rgba(58,92,35,0.3)] lg:grid-cols-[minmax(0,1.1fr)_minmax(0,0.9fr)]">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.checkoutRouteSummaryTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.checkoutRouteSummaryMessage, {
                cartStatus: localizedCartStatus,
                intentStatus: localizedIntentStatus,
                lineCount: cart.items.length,
                addressReady: addressComplete ? copy.readyYes : copy.readyNo,
              })}
            </p>
          </div>
          <div className="grid gap-3 sm:grid-cols-3">
            <div className="rounded-[1rem] bg-[#f7fbef] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.addressReadyLabel}
              </p>
              <p className="mt-2 text-lg font-semibold text-[var(--color-text-primary)]">
                {addressComplete ? copy.readyYes : copy.readyNo}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[#fff7ea] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.intentReadyLabel}
              </p>
              <p className="mt-2 text-lg font-semibold text-[var(--color-text-primary)]">
                {intent ? copy.readyYes : copy.readyNo}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[#eef8ec] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.checkoutPaymentWindowExposureLabel}
              </p>
              <p className="mt-2 text-lg font-semibold text-[var(--color-text-primary)]">
                {formatMoney(combinedExposureMinor, cart.currency, culture)}
              </p>
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

        <div
          id="checkout-overview"
          className="scroll-mt-28 grid gap-5 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)]"
        >
          <section className="rounded-[1rem] border border-[#dce6cf] bg-[linear-gradient(160deg,#ffffff_0%,#f7fbef_100%)] px-6 py-6 shadow-[0_24px_54px_-34px_rgba(58,92,35,0.25)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.checkoutConfidenceTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {canPlaceOrder
                ? copy.checkoutConfidenceReadyMessage
                : copy.checkoutConfidencePendingMessage}
            </p>
            <div className="mt-4 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.checkoutConfidenceLinesLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.checkoutConfidenceLinesValue, {
                    count: cart.items.length,
                  })}
                </p>
              </div>
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.checkoutConfidenceAddressLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {addressComplete
                    ? copy.checkoutConfidenceAddressReady
                    : copy.checkoutConfidenceAddressPending}
                </p>
              </div>
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.checkoutConfidenceShippingLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {hasSelectedShipping
                    ? copy.checkoutConfidenceShippingReady
                    : copy.checkoutConfidenceShippingPending}
                </p>
              </div>
            </div>
          </section>

          <section className="rounded-[1rem] border border-[#dce6cf] bg-[linear-gradient(160deg,#ffffff_0%,#fff7ea_100%)] px-6 py-6 shadow-[0_24px_54px_-34px_rgba(58,92,35,0.25)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.checkoutAttentionTitle}
            </p>
            <div className="mt-4 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.checkoutAttentionBillingLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.checkoutAttentionBillingValue, {
                    count: invoiceAttentionCount,
                  })}
                </p>
              </div>
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.checkoutAttentionPhoneLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {memberProfile?.phoneNumberConfirmed
                    ? copy.checkoutAttentionPhoneReady
                    : copy.checkoutAttentionPhonePending}
                </p>
              </div>
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.checkoutAttentionAddressBookLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {hasSavedAddresses
                    ? formatResource(copy.checkoutAttentionAddressBookReady, {
                        count: memberAddresses.length,
                      })
                    : copy.checkoutAttentionAddressBookMissing}
                </p>
              </div>
            </div>
          </section>
        </div>

        <section
          id="checkout-payment-window"
          className="scroll-mt-28 rounded-[1rem] border border-[#dce6cf] bg-[linear-gradient(150deg,#ffffff_0%,#f7fbef_55%,#fff3dc_100%)] px-6 py-6 shadow-[0_24px_54px_-34px_rgba(58,92,35,0.25)]"
        >
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
            {copy.checkoutPaymentWindowTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {copy.checkoutPaymentWindowMessage}
          </p>
          <div className="mt-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.checkoutPaymentWindowRouteLabel}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {canPlaceOrder
                  ? copy.checkoutPaymentWindowRouteReady
                  : copy.checkoutPaymentWindowRoutePending}
              </p>
            </article>
            <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.checkoutPaymentWindowTotalLabel}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {formatMoney(projectedCheckoutTotalMinor, cart.currency, culture)}
              </p>
            </article>
            <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.checkoutPaymentWindowCarryOverLabel}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {formatMoney(openBillingExposureMinor, cart.currency, culture)}
              </p>
            </article>
            <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.checkoutPaymentWindowAccountLabel}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {hasMemberSession
                  ? copy.checkoutPaymentWindowAccountMember
                  : copy.checkoutPaymentWindowAccountGuest}
              </p>
            </article>
            <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.checkoutPaymentWindowBillingLabel}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {outstandingInvoice
                  ? formatResource(copy.checkoutPaymentWindowBillingValue, {
                      balance: formatMoney(
                        outstandingInvoice.balanceMinor,
                        outstandingInvoice.currency,
                        culture,
                      ),
                    })
                  : copy.checkoutPaymentWindowBillingEmpty}
              </p>
            </article>
            <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.checkoutPaymentWindowExposureLabel}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {formatMoney(combinedExposureMinor, cart.currency, culture)}
              </p>
            </article>
            <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.checkoutPaymentWindowBalanceStateLabel}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {openBillingExposureMinor > 0
                  ? copy.checkoutPaymentWindowBalanceStateAttention
                  : copy.checkoutPaymentWindowBalanceStateClear}
              </p>
            </article>
          </div>
          <div className="mt-6 flex flex-wrap gap-3">
            {outstandingInvoice ? (
              <Link
                href={localizeHref(buildInvoicePath(outstandingInvoice.id), culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.checkoutPaymentWindowInvoicesCta}
              </Link>
            ) : null}
            <Link
              href={localizeHref(hasMemberSession ? "/account" : "/account/sign-in", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.checkoutPaymentWindowAccountCta}
            </Link>
          </div>
        </section>

        <div
          id="checkout-form"
          className="scroll-mt-28 grid gap-8 lg:grid-cols-[minmax(0,1.1fr)_360px]"
        >
          <div className="grid gap-8">
            <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)] sm:px-8">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                  {copy.savedAddressEyebrow}
                </p>
                <h2 className="mt-3 text-3xl font-[family-name:var(--font-display)] text-[var(--color-text-primary)]">
                  {copy.savedAddressTitle}
                </h2>
                <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.savedAddressDescription}
                </p>
              </div>

              {memberAddressesStatus !== "idle" && memberAddressesStatus !== "ok" && (
                <div className="mt-6">
                  <StatusBanner
                    tone="warning"
                    title={copy.savedAddressWarningsTitle}
                    message={formatResource(copy.savedAddressWarningsMessage, {
                      status: memberAddressesStatusLabel,
                    })}
                  />
                </div>
              )}

              <div className="mt-6">
                {hasSavedAddresses ? (
                  <div className="grid gap-4">
                    {memberAddresses.map((address) => {
                      const isSelected = selectedMemberAddressId === address.id;

                      return (
                        <article
                          key={address.id}
                          className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-5 py-5"
                        >
                          <div className="flex flex-wrap items-start justify-between gap-4">
                            <div>
                              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
                                {isSelected
                                  ? copy.savedAddressSelectedLabel
                                  : address.isDefaultShipping
                                    ? copy.savedAddressDefaultShippingLabel
                                    : address.isDefaultBilling
                                      ? copy.savedAddressDefaultBillingLabel
                                      : copy.savedAddressLabel}
                              </p>
                              <p className="mt-3 text-sm font-semibold text-[var(--color-text-primary)]">
                                {address.fullName}
                              </p>
                              {address.company ? (
                                <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
                                  {address.company}
                                </p>
                              ) : null}
                              <div className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                                <p>{address.street1}</p>
                                {address.street2 ? <p>{address.street2}</p> : null}
                                <p>
                                  {address.postalCode} {address.city}
                                </p>
                                {address.state ? <p>{address.state}</p> : null}
                                <p>{address.countryCode}</p>
                                {address.phoneE164 ? <p>{address.phoneE164}</p> : null}
                              </div>
                            </div>
                            <div className="flex flex-col items-start gap-3">
                              <form action={localizeHref("/checkout", culture)}>
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
                                <button
                                  type="submit"
                                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                                >
                                  {isSelected
                                    ? copy.savedAddressSelectedCta
                                    : copy.savedAddressUseCta}
                                </button>
                              </form>
                              <Link
                                href={localizeHref("/account/addresses", culture)}
                                className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                              >
                                {copy.savedAddressManageCta}
                              </Link>
                            </div>
                          </div>
                        </article>
                      );
                    })}
                  </div>
                ) : (
                  <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-5 py-5">
                    <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                      {copy.savedAddressEmptyMessage}
                    </p>
                    {memberProfile ? (
                      <div className="mt-4 rounded-[1rem] bg-[var(--color-surface-panel)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
                          {copy.savedProfilePrefillTitle}
                        </p>
                        <p className="mt-3 font-semibold text-[var(--color-text-primary)]">
                          {[memberProfile.firstName, memberProfile.lastName]
                            .filter(Boolean)
                            .join(" ") || copy.unavailable}
                        </p>
                        <p>{memberProfile.email ?? copy.unavailable}</p>
                        <p>{memberProfile.phoneE164 ?? copy.unavailable}</p>
                        <p className="mt-3">
                          {formatResource(copy.savedProfilePrefillMessage, {
                            status: memberProfileStatusLabel,
                          })}
                        </p>
                        {profilePrefillActive ? (
                          <p className="mt-2 font-medium text-[var(--color-text-primary)]">
                            {copy.savedProfilePrefillActiveMessage}
                          </p>
                        ) : null}
                      </div>
                    ) : null}
                    <div className="mt-4">
                      <Link
                        href={localizeHref("/account/addresses", culture)}
                        className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                      >
                        {copy.savedAddressManageCta}
                      </Link>
                    </div>
                  </div>
                )}
              </div>
            </section>

            {(memberProfile || memberPreferences || memberAddressesStatus !== "idle") && (
              <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)] sm:px-8">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    {copy.memberCheckoutContextEyebrow}
                  </p>
                  <h2 className="mt-3 text-3xl font-[family-name:var(--font-display)] text-[var(--color-text-primary)]">
                    {copy.memberCheckoutContextTitle}
                  </h2>
                  <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {formatResource(copy.memberCheckoutContextDescription, {
                      profileStatus: memberProfileStatusLabel,
                      preferencesStatus: memberPreferencesStatusLabel,
                      addressesStatus: memberAddressesStatusLabel,
                    })}
                  </p>
                </div>

                <div className="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
                  <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {copy.memberCheckoutIdentityLabel}
                    </p>
                    <p className="mt-3 text-sm font-semibold text-[var(--color-text-primary)]">
                      {[memberProfile?.firstName, memberProfile?.lastName]
                        .filter(Boolean)
                        .join(" ") || copy.unavailable}
                    </p>
                    <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {memberProfile?.email ?? copy.unavailable}
                    </p>
                  </article>
                  <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {copy.memberCheckoutPhoneLabel}
                    </p>
                    <p className="mt-3 text-sm font-semibold text-[var(--color-text-primary)]">
                      {memberProfile?.phoneE164 ?? copy.unavailable}
                    </p>
                    <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {memberProfile?.phoneNumberConfirmed
                        ? copy.memberCheckoutPhoneVerified
                        : copy.memberCheckoutPhonePending}
                    </p>
                  </article>
                  <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {copy.memberCheckoutChannelsLabel}
                    </p>
                    <div className="mt-3 space-y-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      <p>{formatResource(copy.memberCheckoutEmailChannel, { value: emailChannelReady ? copy.readyYes : copy.readyNo })}</p>
                      <p>{formatResource(copy.memberCheckoutSmsChannel, { value: smsChannelReady ? copy.readyYes : copy.readyNo })}</p>
                      <p>{formatResource(copy.memberCheckoutWhatsAppChannel, { value: whatsAppChannelReady ? copy.readyYes : copy.readyNo })}</p>
                    </div>
                  </article>
                  <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {copy.memberCheckoutAddressesLabel}
                    </p>
                    <p className="mt-3 text-sm font-semibold text-[var(--color-text-primary)]">
                      {formatResource(copy.memberCheckoutAddressesValue, {
                        count: memberAddresses.length,
                      })}
                    </p>
                    <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {selectedMemberAddressId
                        ? copy.memberCheckoutSelectedAddressReady
                        : copy.memberCheckoutNoSelectedAddress}
                    </p>
                  </article>
                </div>

                <div className="mt-6 flex flex-wrap gap-3">
                  <Link
                    href={localizeHref("/account/profile", culture)}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.memberCheckoutProfileCta}
                  </Link>
                  <Link
                    href={localizeHref("/account/preferences", culture)}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.memberCheckoutPreferencesCta}
                  </Link>
                  <Link
                    href={localizeHref("/account/addresses", culture)}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.savedAddressManageCta}
                  </Link>
                </div>
              </section>
            )}

            {(memberInvoices.length > 0 || memberInvoicesStatus !== "idle") && (
              <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)] sm:px-8">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    {copy.memberCheckoutFinanceEyebrow}
                  </p>
                  <h2 className="mt-3 text-3xl font-[family-name:var(--font-display)] text-[var(--color-text-primary)]">
                    {copy.memberCheckoutFinanceTitle}
                  </h2>
                  <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {formatResource(copy.memberCheckoutFinanceDescription, {
                      invoicesStatus: memberInvoicesStatusLabel,
                      count: memberInvoices.length,
                    })}
                  </p>
                </div>

                <div className="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                  <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {copy.memberCheckoutFinanceOutstandingLabel}
                    </p>
                    <p className="mt-3 text-sm font-semibold text-[var(--color-text-primary)]">
                      {outstandingInvoice
                        ? formatMoney(
                            outstandingInvoice.balanceMinor,
                            outstandingInvoice.currency,
                            culture,
                          )
                        : copy.memberCheckoutFinanceOutstandingEmpty}
                    </p>
                  </article>
                  <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {copy.memberCheckoutFinanceInvoiceLabel}
                    </p>
                    <p className="mt-3 text-sm font-semibold text-[var(--color-text-primary)]">
                      {outstandingInvoice?.orderNumber ?? outstandingInvoice?.id ?? copy.memberCheckoutFinanceInvoiceEmpty}
                    </p>
                  </article>
                  <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {copy.memberCheckoutFinanceDueLabel}
                    </p>
                    <p className="mt-3 text-sm font-semibold text-[var(--color-text-primary)]">
                      {outstandingInvoice
                        ? formatDateTime(outstandingInvoice.dueDateUtc, culture)
                        : copy.memberCheckoutFinanceDueEmpty}
                    </p>
                  </article>
                </div>

                <div className="mt-6 flex flex-wrap gap-3">
                  <Link
                    href={localizeHref("/invoices", culture)}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.memberCheckoutFinanceInvoicesCta}
                  </Link>
                  {outstandingInvoice ? (
                    <Link
                      href={localizeHref(buildInvoicePath(outstandingInvoice.id), culture)}
                      className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                    >
                      {copy.memberCheckoutFinancePrimaryCta}
                    </Link>
                  ) : null}
                </div>
              </section>
            )}

          <form
            action={localizeHref("/checkout", culture)}
            className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)] sm:px-8"
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
                  <input name="fullName" defaultValue={draft.fullName} required autoComplete="name" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  {copy.companyLabel}
                  <input name="company" defaultValue={draft.company} autoComplete="organization" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
                  {copy.street1Label}
                  <input name="street1" defaultValue={draft.street1} required autoComplete="address-line1" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
                  {copy.street2Label}
                  <input name="street2" defaultValue={draft.street2} autoComplete="address-line2" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  {copy.postalCodeLabel}
                  <input name="postalCode" defaultValue={draft.postalCode} required autoComplete="postal-code" inputMode="text" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  {copy.cityLabel}
                  <input name="city" defaultValue={draft.city} required autoComplete="address-level2" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  {copy.stateLabel}
                  <input name="state" defaultValue={draft.state} autoComplete="address-level1" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  {copy.countryCodeLabel}
                  <input name="countryCode" defaultValue={draft.countryCode} required autoComplete="country" autoCapitalize="characters" maxLength={2} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal uppercase text-[var(--color-text-primary)] outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
                  {copy.phoneLabel}
                  <input name="phoneE164" defaultValue={draft.phoneE164} autoComplete="tel" inputMode="tel" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none" />
                </label>
              </div>

              {intent?.requiresShipping && intent.shippingOptions.length > 0 && (
                <div className="grid gap-3 rounded-[1rem] bg-[var(--color-surface-panel-strong)] p-5">
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
                          aria-label={formatResource(copy.shippingOptionAriaLabel, {
                            optionName: option.name,
                          })}
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
                {selectedMemberAddressId ? (
                  <Link
                    href={localizeHref("/account/addresses", culture)}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.savedAddressManageCta}
                  </Link>
                ) : null}
                <Link
                  href={localizeHref("/cart", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.backToCart}
                </Link>
              </div>
            </div>
          </form>

          <div className="flex flex-col gap-5">
            <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.checkoutReadinessTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.checkoutReadinessDescription}
              </p>
              <div className="mt-5 grid gap-3 sm:grid-cols-2">
                {readinessItems.map((item) => (
                  <div
                    key={item.label}
                    className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                  >
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {item.label}
                    </p>
                    <p className="mt-2 text-lg font-semibold text-[var(--color-text-primary)]">
                      {item.value}
                    </p>
                  </div>
                ))}
              </div>
            </aside>

            {!hasMemberSession && (
              <CommerceAuthHandoff
                culture={culture}
                cart={cart}
                returnPath="/checkout"
                routeKey="checkout"
                products={products}
                productsStatus={productsStatus}
              />
            )}

            <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
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

            <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.checkoutLinesTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.checkoutLinesDescription}
              </p>
              <div className="mt-5 flex flex-col gap-4">
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
                    className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                  >
                    <div className="flex items-start gap-4">
                      <div className="flex h-16 w-16 shrink-0 items-center justify-center rounded-[1rem] bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-3">
                        {itemImageUrl ? (
                          // eslint-disable-next-line @next/next/no-img-element
                          <img
                            src={itemImageUrl}
                            alt={itemImageAlt}
                            className="max-h-10 w-auto object-contain"
                          />
                        ) : (
                          <span className="text-[10px] font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                            {copy.noImage}
                          </span>
                        )}
                      </div>
                      <div className="min-w-0 flex-1">
                        <div className="flex items-start justify-between gap-3">
                          <div>
                            <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                              {item.display?.name ?? copy.storefrontVariantFallback}
                            </p>
                            <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
                              {formatResource(copy.lineQuantityLabel, {
                                quantity: item.quantity,
                              })}
                            </p>
                            {(item.selectedAddOns ?? []).length > 0 ? (
                              <div className="mt-2 space-y-1 text-xs text-[var(--color-text-secondary)]">
                                {(item.selectedAddOns ?? []).map((addOn) => (
                                  <p key={addOn.valueId}>
                                    {addOn.optionLabel}: {addOn.valueLabel}
                                    {addOn.priceDeltaMinor !== 0
                                      ? ` (+${formatMoney(addOn.priceDeltaMinor, cart.currency, culture)})`
                                      : ""}
                                  </p>
                                ))}
                              </div>
                            ) : null}
                          </div>
                          <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                            {formatMoney(item.lineGrossMinor, cart.currency, culture)}
                          </p>
                        </div>
                        <div className="mt-3 flex flex-wrap items-center justify-between gap-3 text-xs text-[var(--color-text-secondary)]">
                          <span>
                            {copy.lineTotalShortLabel}:{" "}
                            {formatMoney(item.lineGrossMinor, cart.currency, culture)}
                          </span>
                          {item.display?.href ? (
                            <Link
                              href={localizeHref(itemProductHref, culture)}
                              className="font-semibold text-[var(--color-text-primary)] transition hover:text-[var(--color-brand)]"
                            >
                              {copy.returnToProductCta}
                            </Link>
                          ) : null}
                        </div>
                      </div>
                    </div>
                  </article>
                    );
                  })()
                ))}
              </div>
            </aside>

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

            <div id="checkout-composition" className="scroll-mt-28">
              <CheckoutContentCompositionWindow
                culture={culture}
                hasMemberSession={hasMemberSession}
                canPlaceOrder={canPlaceOrder}
                addressComplete={addressComplete}
                hasSelectedShipping={hasSelectedShipping}
                cartHref={localizeHref("/cart", culture)}
                accountHref={localizeHref("/account", culture)}
                cmsPages={cmsPages}
                categories={categories}
                products={products}
                memberInvoices={memberInvoices}
                projectedCheckoutTotalMinor={projectedCheckoutTotalMinor}
                currency={cart.currency}
              />
            </div>

            <CommerceStorefrontWindow
              culture={culture}
              cmsPages={cmsPages}
              cmsPagesStatus={cmsPagesStatus}
              categories={categories}
              categoriesStatus={categoriesStatus}
              products={products}
              productsStatus={productsStatus}
            />

            <CommerceContinuationRail culture={culture} includeCart />
          </div>
        </div>
        </div>
      </div>
    </section>
  );
}
