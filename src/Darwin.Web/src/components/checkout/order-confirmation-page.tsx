import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { createStorefrontPaymentIntentAction } from "@/features/checkout/actions";
import type { PublicStorefrontOrderConfirmation } from "@/features/checkout/types";
import {
  formatResource,
  getCommerceResource,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { parseAddressJson, type ParsedAddress } from "@/lib/address-json";
import { buildOrderPath } from "@/lib/entity-paths";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { buildLocalizedAuthHref, localizeHref, sanitizeAppPath } from "@/lib/locale-routing";

type OrderConfirmationPageProps = {
  culture: string;
  confirmation: PublicStorefrontOrderConfirmation | null;
  status: string;
  message?: string;
  checkoutStatus?: string;
  paymentCompletionStatus?: string;
  paymentOutcome?: string;
  paymentError?: string;
  cancelled?: boolean;
  hasMemberSession?: boolean;
};

function renderAddress(address: ParsedAddress | null, culture: string) {
  const copy = getCommerceResource(culture);

  if (!address) {
    return (
      <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
        {copy.snapshotUnavailable}
      </p>
    );
  }

  return (
    <div className="text-sm leading-7 text-[var(--color-text-secondary)]">
      <p className="font-semibold text-[var(--color-text-primary)]">
        {address.fullName || copy.recipientUnavailable}
      </p>
      {address.company ? <p>{address.company}</p> : null}
      <p>{address.street1}</p>
      {address.street2 ? <p>{address.street2}</p> : null}
      <p>
        {address.postalCode} {address.city}
      </p>
      {address.state ? <p>{address.state}</p> : null}
      <p>{address.countryCode}</p>
      {address.phoneE164 ? <p>{address.phoneE164}</p> : null}
    </div>
  );
}

function hasSuccessfulPayment(confirmation: PublicStorefrontOrderConfirmation) {
  return confirmation.payments.some((payment) => {
    const currentStatus = payment.status.toLowerCase();
    return currentStatus === "paid" ||
      currentStatus === "succeeded" ||
      currentStatus === "completed";
  });
}

function getRecordedPaymentAmountMinor(
  confirmation: PublicStorefrontOrderConfirmation,
) {
  return confirmation.payments.reduce((total, payment) => {
    const currentStatus = payment.status.toLowerCase();
    const isRecorded =
      currentStatus === "paid" ||
      currentStatus === "succeeded" ||
      currentStatus === "completed";

    return isRecorded ? total + payment.amountMinor : total;
  }, 0);
}

function localizeOrderStatus(status: string | undefined, culture: string) {
  const normalized = (status ?? "").trim().toLowerCase();
  const english = culture.toLowerCase().startsWith("en");

  if (!normalized) {
    return english ? "Unavailable" : "Nicht verfuegbar";
  }

  const labels: Record<string, { de: string; en: string }> = {
    created: { de: "Erstellt", en: "Created" },
    confirmed: { de: "Bestaetigt", en: "Confirmed" },
    paid: { de: "Bezahlt", en: "Paid" },
    partiallyshipped: { de: "Teilweise versendet", en: "Partially shipped" },
    shipped: { de: "Versendet", en: "Shipped" },
    delivered: { de: "Zugestellt", en: "Delivered" },
    cancelled: { de: "Storniert", en: "Cancelled" },
    refunded: { de: "Erstattet", en: "Refunded" },
    partiallyrefunded: { de: "Teilweise erstattet", en: "Partially refunded" },
    completed: { de: "Abgeschlossen", en: "Completed" },
  };

  const label = labels[normalized.replace(/[\s_-]/g, "")];
  return label ? (english ? label.en : label.de) : normalized;
}

function localizePaymentStatus(status: string | undefined, culture: string) {
  const normalized = (status ?? "").trim().toLowerCase();
  const english = culture.toLowerCase().startsWith("en");

  if (!normalized) {
    return english ? "Unavailable" : "Nicht verfuegbar";
  }

  const labels: Record<string, { de: string; en: string }> = {
    pending: { de: "Ausstehend", en: "Pending" },
    authorized: { de: "Autorisiert", en: "Authorized" },
    paid: { de: "Bezahlt", en: "Paid" },
    succeeded: { de: "Erfolgreich", en: "Succeeded" },
    completed: { de: "Abgeschlossen", en: "Completed" },
    failed: { de: "Fehlgeschlagen", en: "Failed" },
    cancelled: { de: "Abgebrochen", en: "Cancelled" },
    refunded: { de: "Erstattet", en: "Refunded" },
    partiallyrefunded: { de: "Teilweise erstattet", en: "Partially refunded" },
  };

  const label = labels[normalized.replace(/[\s_-]/g, "")];
  return label ? (english ? label.en : label.de) : normalized;
}

function resolveDisplayedPaymentStatus(
  confirmation: PublicStorefrontOrderConfirmation,
  paymentCompletionStatus: string | undefined,
  paymentOutcome: string | undefined,
  fallback: string,
) {
  if (paymentCompletionStatus === "completed" && paymentOutcome) {
    return paymentOutcome;
  }

  const latestAttempt = [...confirmation.payments].sort((left, right) => {
    const leftTimestamp = Date.parse(left.createdAtUtc);
    const rightTimestamp = Date.parse(right.createdAtUtc);

    if (!Number.isNaN(leftTimestamp) && !Number.isNaN(rightTimestamp)) {
      return rightTimestamp - leftTimestamp;
    }

    return right.id.localeCompare(left.id);
  })[0];

  return latestAttempt?.status ?? fallback;
}

export function OrderConfirmationPage({
  culture,
  confirmation,
  message,
  checkoutStatus,
  paymentCompletionStatus,
  paymentOutcome,
  paymentError,
  cancelled,
  hasMemberSession = false,
}: OrderConfirmationPageProps) {
  const copy = getCommerceResource(culture);
  const resolvedPaymentError = resolveLocalizedQueryMessage(paymentError, copy);
  const resolvedMessage = resolveLocalizedQueryMessage(message, copy);

  if (!confirmation) {
    return (
      <section className="mx-auto flex w-full max-w-[1180px] flex-1 px-5 py-10 sm:px-6 lg:px-8">
        <div className="w-full rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)]">
          <StatusBanner
            tone="warning"
            title={copy.orderConfirmationUnavailableTitle}
            message={resolvedMessage ?? copy.orderConfirmationUnavailableMessage}
          />
          <div className="mt-6 flex flex-wrap gap-3">
            <Link
              href={localizeHref("/catalog", culture)}
              className="rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)]"
            >
              {copy.continueShopping}
            </Link>
            <Link
              href={localizeHref("/account", culture)}
              className="rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold"
            >
              {copy.confirmationOpenAccountHubCta}
            </Link>
          </div>
        </div>
      </section>
    );
  }

  const billingAddress = parseAddressJson(confirmation.billingAddressJson);
  const shippingAddress = parseAddressJson(confirmation.shippingAddressJson);
  const paid = hasSuccessfulPayment(confirmation);
  const recordedPaymentAmountMinor = getRecordedPaymentAmountMinor(confirmation);
  const remainingPaymentAmountMinor = Math.max(
    confirmation.grandTotalGrossMinor - recordedPaymentAmountMinor,
    0,
  );
  const paymentNeedsAttention = !paid || remainingPaymentAmountMinor > 0;
  const memberOrderDetailHref = sanitizeAppPath(
    buildOrderPath(confirmation.orderId),
    "/orders",
  );
  const memberOrdersHref = "/orders";
  const signInHref = buildLocalizedAuthHref(
    "/account/sign-in",
    memberOrderDetailHref,
    culture,
    "/orders",
  );
  const registerHref = buildLocalizedAuthHref(
    "/account/register",
    memberOrderDetailHref,
    culture,
    "/orders",
  );
  const displayedPaymentStatus = resolveDisplayedPaymentStatus(
    confirmation,
    paymentCompletionStatus,
    paymentOutcome,
    copy.unavailable,
  );
  const localizedOrderStatus = localizeOrderStatus(confirmation.status, culture);
  const localizedPaymentStatus = localizePaymentStatus(displayedPaymentStatus, culture);

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
            {copy.commerceBreadcrumbConfirmation}
          </span>
        </nav>

        <header className="rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_48%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.orderConfirmationEyebrow}
          </p>
          <div className="mt-4 flex flex-wrap items-end justify-between gap-6">
            <div>
              <h1 className="font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
                {formatResource(copy.orderConfirmationTitle, {
                  orderNumber: confirmation.orderNumber,
                })}
              </h1>
              <p className="mt-4 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)]">
                {copy.orderConfirmationDescription}
              </p>
            </div>
            <div className="rounded-[1rem] bg-white/80 px-5 py-4 text-right shadow-[0_20px_40px_-30px_rgba(58,92,35,0.45)]">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.grandTotalLabel}
              </p>
              <p className="mt-2 text-2xl font-semibold text-[var(--color-text-primary)]">
                {formatMoney(confirmation.grandTotalGrossMinor, confirmation.currency, culture)}
              </p>
            </div>
          </div>
        </header>

        {checkoutStatus === "order-placed" ? (
          <StatusBanner title={copy.orderPlacedTitle} message={copy.orderPlacedMessage} />
        ) : null}

        {paymentCompletionStatus === "completed" ? (
          <StatusBanner
            title={
              paymentOutcome === "Cancelled"
                ? copy.paymentCancelledTitle
                : copy.paymentReconciledTitle
            }
            message={formatResource(copy.paymentReconciledMessage, {
              orderStatus: localizedOrderStatus,
              paymentStatus: localizedPaymentStatus,
            })}
          />
        ) : null}

        {resolvedPaymentError || cancelled || paymentCompletionStatus === "failed" ? (
          <StatusBanner
            tone="warning"
            title={copy.nextStepPaymentRetryTitle}
            message={resolvedPaymentError ?? copy.nextStepPaymentRetryMessage}
          />
        ) : null}

        <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_360px]">
          <div className="flex flex-col gap-6">
            <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <h2 className="text-xl font-semibold text-[var(--color-text-primary)]">
                {copy.summaryTitle}
              </h2>
              <div className="mt-5 grid gap-4 sm:grid-cols-2">
                <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.statusLabel}
                  </p>
                  <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                    {localizedOrderStatus}
                  </p>
                </div>
                <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.paymentsTitle}
                  </p>
                  <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                    {localizedPaymentStatus}
                  </p>
                </div>
                <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.createdLabel}
                  </p>
                  <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                    {formatDateTime(confirmation.createdAtUtc, culture)}
                  </p>
                </div>
                <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.referenceLabel}
                  </p>
                  <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                    {confirmation.orderNumber}
                  </p>
                </div>
              </div>
            </section>

            <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <h2 className="text-xl font-semibold text-[var(--color-text-primary)]">
                {copy.checkoutLinesTitle}
              </h2>
              <div className="mt-5 flex flex-col gap-4">
                {confirmation.lines.map((line) => (
                  <article
                    key={line.id}
                    className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                  >
                    <div className="flex flex-wrap items-start justify-between gap-4">
                      <div>
                        <p className="font-semibold text-[var(--color-text-primary)]">
                          {line.name}
                        </p>
                        <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
                          {formatResource(copy.lineQuantityLabel, {
                            quantity: line.quantity,
                          })}
                        </p>
                        <p className="mt-1 text-xs text-[var(--color-text-muted)]">
                          {line.sku}
                        </p>
                      </div>
                      <p className="font-semibold text-[var(--color-text-primary)]">
                        {formatMoney(line.lineGrossMinor, confirmation.currency, culture)}
                      </p>
                    </div>
                  </article>
                ))}
              </div>
            </section>

            <section className="grid gap-6 md:grid-cols-2">
              <article className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">
                  {copy.billingAddressTitle}
                </h2>
                <div className="mt-4">
                  {renderAddress(billingAddress, culture)}
                </div>
              </article>
              <article className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">
                  {copy.shippingAddressTitle}
                </h2>
                <div className="mt-4">
                  {renderAddress(shippingAddress, culture)}
                </div>
              </article>
            </section>
          </div>

          <aside className="flex h-fit flex-col gap-5">
            <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <h2 className="text-xl font-semibold text-[var(--color-text-primary)]">
                {copy.paymentsTitle}
              </h2>
              <div className="mt-5 space-y-3 text-sm text-[var(--color-text-secondary)]">
                <div className="flex items-center justify-between">
                  <span>{copy.grandTotalLabel}</span>
                  <span>{formatMoney(confirmation.grandTotalGrossMinor, confirmation.currency, culture)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>{copy.paidLabel}</span>
                  <span>{formatMoney(recordedPaymentAmountMinor, confirmation.currency, culture)}</span>
                </div>
                <div className="flex items-center justify-between border-t border-[var(--color-border-soft)] pt-3 text-base font-semibold text-[var(--color-text-primary)]">
                  <span>{copy.confirmationPaymentRemainingLabel}</span>
                  <span>{formatMoney(remainingPaymentAmountMinor, confirmation.currency, culture)}</span>
                </div>
              </div>

              {paymentNeedsAttention ? (
                <form action={createStorefrontPaymentIntentAction} className="mt-6">
                  <input type="hidden" name="orderId" value={confirmation.orderId} />
                  <input type="hidden" name="orderNumber" value={confirmation.orderNumber} />
                  <button
                    type="submit"
                    className="w-full rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                  >
                    {copy.continueToPayment}
                  </button>
                </form>
              ) : (
                <div className="mt-6">
                  <StatusBanner title={copy.paymentRecordedTitle} message={copy.paymentRecordedMessage} />
                </div>
              )}
            </section>

            {(confirmation.shippingMethodName || confirmation.shippingCarrier || confirmation.shippingService) ? (
              <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">
                  {copy.shippingSnapshotTitle}
                </h2>
                <div className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  <p>{confirmation.shippingMethodName ?? copy.methodUnavailable}</p>
                  <p>
                    {confirmation.shippingCarrier ?? copy.carrierUnavailable}
                    {confirmation.shippingService ? ` / ${confirmation.shippingService}` : ""}
                  </p>
                  <p>{formatMoney(confirmation.shippingTotalMinor, confirmation.currency, culture)}</p>
                </div>
              </section>
            ) : null}

            <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">
                {copy.nextStepsTitle}
              </h2>
              <div className="mt-5 flex flex-col gap-3">
                {hasMemberSession ? (
                  <Link
                    href={localizeHref(memberOrdersHref, culture)}
                    className="inline-flex justify-center rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                  >
                    {copy.openOrdersCta}
                  </Link>
                ) : (
                  <>
                    <Link
                      href={signInHref}
                      className="inline-flex justify-center rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                    >
                      {copy.signInForTrackingCta}
                    </Link>
                    <Link
                      href={registerHref}
                      className="inline-flex justify-center rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold transition hover:bg-[var(--color-surface-panel-strong)]"
                    >
                      {copy.createAccountForTrackingCta}
                    </Link>
                  </>
                )}
                <Link
                  href={localizeHref("/catalog", culture)}
                  className="inline-flex justify-center rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.continueShopping}
                </Link>
              </div>
            </section>
          </aside>
        </div>
      </div>
    </section>
  );
}
