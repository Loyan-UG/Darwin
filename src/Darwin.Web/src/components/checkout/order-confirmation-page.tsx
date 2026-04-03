import Link from "next/link";
import { CommerceContinuationRail } from "@/components/checkout/commerce-continuation-rail";
import { CommerceStorefrontWindow } from "@/components/checkout/commerce-storefront-window";
import { StatusBanner } from "@/components/feedback/status-banner";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import {
  getProductSavingsPercent,
  sortProductsByOpportunity,
} from "@/features/catalog/merchandising";
import type { PublicPageSummary } from "@/features/cms/types";
import { createStorefrontPaymentIntentAction } from "@/features/checkout/actions";
import type { PublicStorefrontOrderConfirmation } from "@/features/checkout/types";
import type {
  MemberInvoiceSummary,
  MemberOrderSummary,
  MyLoyaltyOverview,
} from "@/features/member-portal/types";
import {
  formatResource,
  getCommerceResource,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { parseAddressJson, type ParsedAddress } from "@/lib/address-json";
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
  memberOrders: MemberOrderSummary[];
  memberOrdersStatus: string;
  memberInvoices: MemberInvoiceSummary[];
  memberInvoicesStatus: string;
  memberLoyaltyOverview: MyLoyaltyOverview | null;
  memberLoyaltyStatus: string;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
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
    return currentStatus === "paid"
      || currentStatus === "succeeded"
      || currentStatus === "completed";
  });
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

  if (confirmation.payments.length === 1) {
    return confirmation.payments[0].status;
  }

  return fallback;
}

export function OrderConfirmationPage({
  culture,
  confirmation,
  status,
  message,
  checkoutStatus,
  paymentCompletionStatus,
  paymentOutcome,
  paymentError,
  cancelled,
  hasMemberSession = false,
  memberOrders,
  memberOrdersStatus,
  memberInvoices,
  memberInvoicesStatus,
  memberLoyaltyOverview,
  memberLoyaltyStatus,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
}: OrderConfirmationPageProps) {
  const copy = getCommerceResource(culture);
  const resolvedPaymentError = resolveLocalizedQueryMessage(paymentError, copy);
  const resolvedMessage = resolveLocalizedQueryMessage(message, copy);

  if (!confirmation) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
        <div className="w-full rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title={copy.orderConfirmationUnavailableTitle}
            message={resolvedMessage ?? formatResource(copy.orderConfirmationUnavailableMessage, {
              status,
            })}
          />
          <div className="mt-8">
            <CommerceContinuationRail
              culture={culture}
              includeCms={false}
              includeCart={false}
            />
          </div>
        </div>
      </section>
    );
  }

  const billingAddress = parseAddressJson(confirmation.billingAddressJson);
  const shippingAddress = parseAddressJson(confirmation.shippingAddressJson);
  const paid = hasSuccessfulPayment(confirmation);
  const paymentNeedsAttention = !paid;
  const memberOrderDetailHref = sanitizeAppPath(
    `/orders/${confirmation.orderId}`,
    "/orders",
  );
  const memberOrdersHref = sanitizeAppPath(
    "/orders",
    "/orders",
  );
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
  const paymentStepTitle = resolvedPaymentError || cancelled || paymentCompletionStatus === "failed"
    ? copy.nextStepPaymentRetryTitle
    : paid
      ? copy.nextStepPaymentDoneTitle
      : copy.nextStepPaymentPendingTitle;
  const paymentStepMessage = resolvedPaymentError || cancelled || paymentCompletionStatus === "failed"
    ? copy.nextStepPaymentRetryMessage
    : paid
      ? copy.nextStepPaymentDoneMessage
      : copy.nextStepPaymentPendingMessage;
  const accountStepTitle = hasMemberSession
    ? copy.nextStepAccountSignedInTitle
    : copy.nextStepAccountGuestTitle;
  const accountStepMessage = hasMemberSession
    ? copy.nextStepAccountSignedInMessage
    : copy.nextStepAccountGuestMessage;
  const displayedPaymentStatus = resolveDisplayedPaymentStatus(
    confirmation,
    paymentCompletionStatus,
    paymentOutcome,
    copy.unavailable,
  );
  const outstandingInvoice =
    memberInvoices.find((invoice) => invoice.balanceMinor > 0) ??
    memberInvoices[0] ??
    null;
  const latestMemberOrder = memberOrders[0] ?? null;
  const loyaltyFocus =
    [...(memberLoyaltyOverview?.accounts ?? [])].sort((left, right) => {
      const leftRank = left.pointsToNextReward ?? Number.MAX_SAFE_INTEGER;
      const rightRank = right.pointsToNextReward ?? Number.MAX_SAFE_INTEGER;
      return leftRank - rightRank;
    })[0] ?? null;
  const purchasedNames = new Set(
    confirmation.lines.map((line) => line.name.trim().toLowerCase()),
  );
  const guestOfferBoard = sortProductsByOpportunity(
    products.filter((product) => !purchasedNames.has(product.name.trim().toLowerCase())),
  ).slice(0, 3);

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
          <Link href={localizeHref("/cart", culture)} className="transition hover:text-[var(--color-brand)]">
            {copy.commerceBreadcrumbCart}
          </Link>
          <span>/</span>
          <Link href={localizeHref("/checkout", culture)} className="transition hover:text-[var(--color-brand)]">
            {copy.commerceBreadcrumbCheckout}
          </Link>
          <span>/</span>
          <span className="font-medium text-[var(--color-text-primary)]">
            {copy.commerceBreadcrumbConfirmation}
          </span>
        </nav>

        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.orderConfirmationEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {formatResource(copy.orderConfirmationTitle, {
              orderNumber: confirmation.orderNumber,
            })}
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {copy.orderConfirmationDescription}
          </p>
        </div>

        {checkoutStatus === "order-placed" && (
          <StatusBanner
            title={copy.orderPlacedTitle}
            message={copy.orderPlacedMessage}
          />
        )}

        {paymentCompletionStatus === "completed" && (
          <StatusBanner
            title={
              paymentOutcome === "Cancelled"
                ? copy.paymentCancelledTitle
                : copy.paymentReconciledTitle
            }
            message={formatResource(copy.paymentReconciledMessage, {
              orderStatus: confirmation.status,
              paymentStatus: displayedPaymentStatus,
            })}
          />
        )}

        {paymentCompletionStatus === "missing-context" && (
          <StatusBanner
            tone="warning"
            title={copy.missingContextTitle}
            message={copy.missingContextMessage}
          />
        )}

        {cancelled && (
          <StatusBanner
            tone="warning"
            title={copy.hostedCheckoutCancelledTitle}
            message={copy.hostedCheckoutCancelledMessage}
          />
        )}

        {resolvedPaymentError && (
          <StatusBanner
            tone="warning"
            title={
              paymentCompletionStatus === "failed"
                ? copy.paymentCompletionFailedTitle
                : copy.paymentHandoffFailedTitle
            }
            message={resolvedPaymentError}
          />
        )}

        {status !== "ok" && (
          <StatusBanner
            tone="warning"
            title={copy.confirmationWarningsTitle}
            message={resolvedMessage ?? formatResource(copy.confirmationWarningsMessage, {
              status,
            })}
          />
        )}

        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
            {copy.confirmationRouteSummaryTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.confirmationRouteSummaryMessage, {
              status,
              paymentCompletionStatus: paymentCompletionStatus ?? "idle",
              paymentStatus: displayedPaymentStatus,
            })}
          </p>
        </div>

        <div className="grid gap-5 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)]">
          <section className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.confirmationCareTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {paymentNeedsAttention
                ? copy.confirmationCareAttentionMessage
                : copy.confirmationCareStableMessage}
            </p>
            <div className="mt-4 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.confirmationCarePaymentLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {paymentNeedsAttention
                    ? copy.confirmationCarePaymentPending
                    : copy.confirmationCarePaymentDone}
                </p>
              </div>
              <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.confirmationCareReferenceLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {confirmation.orderNumber}
                </p>
              </div>
              <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.confirmationCareStatusLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {confirmation.status}
                </p>
              </div>
            </div>
          </section>

          <section className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.confirmationNextWindowTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {hasMemberSession
                ? copy.confirmationNextWindowMemberMessage
                : copy.confirmationNextWindowGuestMessage}
            </p>
            <div className="mt-4 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.confirmationNextWindowOrdersLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {latestMemberOrder
                    ? formatResource(copy.confirmationNextWindowOrdersValue, {
                        orderNumber: latestMemberOrder.orderNumber,
                      })
                    : copy.confirmationNextWindowOrdersFallback}
                </p>
              </div>
              <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.confirmationNextWindowBillingLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {outstandingInvoice
                    ? formatResource(copy.confirmationNextWindowBillingValue, {
                        balance: formatMoney(
                          outstandingInvoice.balanceMinor,
                          outstandingInvoice.currency,
                          culture,
                        ),
                      })
                    : copy.confirmationNextWindowBillingFallback}
                </p>
              </div>
              <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.confirmationNextWindowLoyaltyLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {loyaltyFocus
                    ? formatResource(copy.confirmationNextWindowLoyaltyValue, {
                        business: loyaltyFocus.businessName,
                      })
                    : copy.confirmationNextWindowLoyaltyFallback}
                </p>
              </div>
            </div>
          </section>
        </div>

        <div className="grid gap-8 lg:grid-cols-[minmax(0,1.05fr)_360px]">
          <div className="flex flex-col gap-6">
            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <div className="grid gap-5 sm:grid-cols-2">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    {copy.billingAddressTitle}
                  </p>
                  <div className="mt-3">{renderAddress(billingAddress, culture)}</div>
                </div>
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    {copy.shippingAddressTitle}
                  </p>
                  <div className="mt-3">{renderAddress(shippingAddress, culture)}</div>
                </div>
              </div>
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.orderLinesTitle}
              </p>
              <div className="mt-5 flex flex-col gap-4">
                {confirmation.lines.map((line) => (
                  <article
                    key={line.id}
                    className="rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                  >
                    <div className="flex flex-wrap items-start justify-between gap-4">
                      <div>
                        <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">
                          {line.name}
                        </h2>
                        <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                          {formatResource(copy.skuQtyLine, {
                            sku: line.sku,
                            quantity: line.quantity,
                          })}
                        </p>
                      </div>
                      <div className="text-right text-sm leading-7 text-[var(--color-text-secondary)]">
                        <p>
                          {formatMoney(line.unitPriceGrossMinor, confirmation.currency, culture)} {copy.eachSuffix}
                        </p>
                        <p className="font-semibold text-[var(--color-text-primary)]">
                          {formatMoney(line.lineGrossMinor, confirmation.currency, culture)}
                        </p>
                      </div>
                    </div>
                  </article>
                ))}
              </div>
            </div>
          </div>

          <div className="flex flex-col gap-5">
            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.nextStepsTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.nextStepsDescription}
              </p>
              <div className="mt-5 grid gap-3">
                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
                    {copy.nextStepsPaymentLabel}
                  </p>
                  <h2 className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {paymentStepTitle}
                  </h2>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {paymentStepMessage}
                  </p>
                </article>

                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
                    {copy.nextStepsAccountLabel}
                  </p>
                  <h2 className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {accountStepTitle}
                  </h2>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {accountStepMessage}
                  </p>
                </article>

                <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
                    {copy.nextStepsReferenceLabel}
                  </p>
                  <h2 className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                    {formatResource(copy.nextStepReferenceTitle, {
                      orderNumber: confirmation.orderNumber,
                    })}
                  </h2>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {copy.nextStepReferenceMessage}
                  </p>
                </article>
              </div>

              <div className="mt-6 flex flex-wrap gap-3">
                {paymentNeedsAttention ? (
                  <form action={createStorefrontPaymentIntentAction}>
                    <input type="hidden" name="orderId" value={confirmation.orderId} />
                    <input type="hidden" name="orderNumber" value={confirmation.orderNumber} />
                    <button
                      type="submit"
                      className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                    >
                      {copy.continueToPayment}
                    </button>
                  </form>
                ) : null}

                {hasMemberSession ? (
                  <Link
                    href={localizeHref(memberOrdersHref, culture)}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.openOrdersCta}
                  </Link>
                ) : (
                  <>
                    <Link
                      href={localizeHref("/account", culture)}
                      className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                    >
                      {copy.confirmationOpenAccountHubCta}
                    </Link>
                  </>
                )}

                <Link
                  href={localizeHref("/catalog", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.continueShopping}
                </Link>
              </div>
            </aside>

            {hasMemberSession ? (
              <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                  {copy.confirmationMemberWindowTitle}
                </p>
                <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {formatResource(copy.confirmationMemberWindowMessage, {
                    ordersStatus: memberOrdersStatus,
                    invoicesStatus: memberInvoicesStatus,
                    loyaltyStatus: memberLoyaltyStatus,
                  })}
                </p>
                <div className="mt-5 grid gap-3">
                  <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {copy.confirmationMemberOrdersLabel}
                    </p>
                    <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                      {memberOrders[0]
                        ? formatResource(copy.confirmationMemberOrdersValue, {
                            orderNumber: memberOrders[0].orderNumber,
                          })
                        : copy.confirmationMemberOrdersEmpty}
                    </p>
                  </article>
                  <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {copy.confirmationMemberInvoicesLabel}
                    </p>
                    <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                      {outstandingInvoice
                        ? formatResource(copy.confirmationMemberInvoicesValue, {
                            balance: formatMoney(
                              outstandingInvoice.balanceMinor,
                              outstandingInvoice.currency,
                              culture,
                            ),
                          })
                        : copy.confirmationMemberInvoicesEmpty}
                    </p>
                  </article>
                  <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {copy.confirmationMemberLoyaltyLabel}
                    </p>
                    <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                      {loyaltyFocus
                        ? formatResource(copy.confirmationMemberLoyaltyValue, {
                            business: loyaltyFocus.businessName,
                          })
                        : copy.confirmationMemberLoyaltyEmpty}
                    </p>
                  </article>
                </div>
                <div className="mt-6 flex flex-wrap gap-3">
                  <Link
                    href={localizeHref(memberOrdersHref, culture)}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.openOrdersCta}
                  </Link>
                  <Link
                    href={localizeHref("/invoices", culture)}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.confirmationOpenInvoicesCta}
                  </Link>
                  <Link
                    href={localizeHref("/loyalty", culture)}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.confirmationOpenLoyaltyCta}
                  </Link>
                </div>
              </aside>
            ) : (
              <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                  {copy.confirmationGuestWindowTitle}
                </p>
                <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {formatResource(copy.confirmationGuestWindowMessage, {
                    returnPath: memberOrderDetailHref,
                  })}
                </p>
                <div className="mt-5 grid gap-3">
                  <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {copy.confirmationGuestReferenceLabel}
                    </p>
                    <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                      {formatResource(copy.confirmationGuestReferenceValue, {
                        orderNumber: confirmation.orderNumber,
                      })}
                    </p>
                  </article>
                  <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {copy.confirmationGuestReturnLabel}
                    </p>
                    <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                      {memberOrderDetailHref}
                    </p>
                  </article>
                  <article className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                    <div className="flex items-center justify-between gap-3">
                      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                        {copy.confirmationGuestOfferBoardTitle}
                      </p>
                      <Link
                        href={localizeHref("/catalog", culture)}
                        className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                      >
                        {copy.confirmationGuestOfferBoardCta}
                      </Link>
                    </div>
                    <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {formatResource(copy.confirmationGuestOfferBoardMessage, {
                        productCount: guestOfferBoard.length,
                      })}
                    </p>
                    {guestOfferBoard.length > 0 ? (
                      <div className="mt-3 grid gap-3">
                        {guestOfferBoard.map((product) => {
                          const savingsPercent = getProductSavingsPercent(product);

                          return (
                            <div
                              key={product.id}
                              className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3"
                            >
                              <Link
                                href={localizeHref(`/catalog/${product.slug}`, culture)}
                                className="font-semibold text-[var(--color-text-primary)] transition hover:text-[var(--color-brand)]"
                              >
                                {product.name}
                              </Link>
                              <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                                {savingsPercent !== null
                                  ? formatResource(copy.confirmationGuestOfferBoardOfferDescription, {
                                      savingsPercent,
                                      price: formatMoney(
                                        product.priceMinor,
                                        product.currency,
                                        culture,
                                      ),
                                    })
                                  : product.shortDescription ??
                                    copy.confirmationGuestOfferBoardFallbackDescription}
                              </p>
                            </div>
                          );
                        })}
                      </div>
                    ) : (
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {copy.confirmationGuestOfferBoardEmptyMessage}
                      </p>
                    )}
                  </article>
                </div>
                <div className="mt-6 flex flex-wrap gap-3">
                  <Link
                    href={signInHref}
                    className="inline-flex rounded-full bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                  >
                    {copy.signInForTrackingCta}
                  </Link>
                  <Link
                    href={registerHref}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.createAccountForTrackingCta}
                  </Link>
                  <Link
                    href={buildLocalizedAuthHref("/account/activation", memberOrderDetailHref, culture, "/orders")}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.commerceAuthActivationCta}
                  </Link>
                  <Link
                    href={buildLocalizedAuthHref("/account/password", memberOrderDetailHref, culture, "/orders")}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.commerceAuthPasswordCta}
                  </Link>
                </div>
              </aside>
            )}

            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.summaryTitle}
              </p>
              <div className="mt-5 space-y-3 text-sm text-[var(--color-text-secondary)]">
                <div className="flex items-center justify-between">
                  <span>{copy.statusLabel}</span>
                  <span>{confirmation.status}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>{copy.createdLabel}</span>
                  <span>{formatDateTime(confirmation.createdAtUtc, culture)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>{copy.subtotalNetLabel}</span>
                  <span>{formatMoney(confirmation.subtotalNetMinor, confirmation.currency, culture)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>{copy.taxTotalLabel}</span>
                  <span>{formatMoney(confirmation.taxTotalMinor, confirmation.currency, culture)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>{copy.shippingLabel}</span>
                  <span>{formatMoney(confirmation.shippingTotalMinor, confirmation.currency, culture)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>{copy.discountLabel}</span>
                  <span>{formatMoney(confirmation.discountTotalMinor, confirmation.currency, culture)}</span>
                </div>
                <div className="flex items-center justify-between border-t border-[var(--color-border-soft)] pt-3 text-base font-semibold text-[var(--color-text-primary)]">
                  <span>{copy.grandTotalLabel}</span>
                  <span>{formatMoney(confirmation.grandTotalGrossMinor, confirmation.currency, culture)}</span>
                </div>
              </div>

              {(confirmation.shippingMethodName || confirmation.shippingCarrier || confirmation.shippingService) && (
                <div className="mt-6 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  <p className="font-semibold text-[var(--color-text-primary)]">{copy.shippingSnapshotTitle}</p>
                  <p>{confirmation.shippingMethodName ?? copy.methodUnavailable}</p>
                  <p>
                    {confirmation.shippingCarrier ?? copy.carrierUnavailable}
                    {confirmation.shippingService ? ` / ${confirmation.shippingService}` : ""}
                  </p>
                </div>
              )}
            </aside>

            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.paymentsTitle}
              </p>
              <div className="mt-4 flex flex-col gap-3">
                {confirmation.payments.length > 0 ? (
                  confirmation.payments.map((payment) => (
                    <div
                      key={payment.id}
                      className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]"
                    >
                      <p className="font-semibold text-[var(--color-text-primary)]">
                        {payment.provider} - {payment.status}
                      </p>
                      <p>{formatMoney(payment.amountMinor, payment.currency, culture)}</p>
                      <p>{copy.referenceLabel} {payment.providerReference ?? copy.unavailable}</p>
                      {payment.paidAtUtc ? <p>{copy.paidLabel} {formatDateTime(payment.paidAtUtc, culture)}</p> : null}
                    </div>
                  ))
                ) : (
                  <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                    {copy.noPaymentAttemptsMessage}
                  </p>
                )}
              </div>

              {paid && (
                <div className="mt-6">
                  <StatusBanner
                    title={copy.paymentRecordedTitle}
                    message={copy.paymentRecordedMessage}
                  />
                </div>
              )}
            </aside>

            <CommerceStorefrontWindow
              culture={culture}
              cmsPages={cmsPages}
              cmsPagesStatus={cmsPagesStatus}
              categories={categories}
              categoriesStatus={categoriesStatus}
              products={products}
              productsStatus={productsStatus}
              title={copy.confirmationStorefrontWindowTitle}
              description={copy.confirmationStorefrontWindowMessage}
            />

            <CommerceContinuationRail
              culture={culture}
              includeCart={false}
            />
          </div>
        </div>
      </div>
    </section>
  );
}
