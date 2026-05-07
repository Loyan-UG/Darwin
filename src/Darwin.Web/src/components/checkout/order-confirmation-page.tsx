import Link from "next/link";
import { ConfirmationContentCompositionWindow } from "@/components/checkout/confirmation-content-composition-window";
import { CommerceContinuationRail } from "@/components/checkout/commerce-continuation-rail";
import { CommerceStorefrontWindow } from "@/components/checkout/commerce-storefront-window";
import { StorefrontCampaignBoard } from "@/components/storefront/storefront-campaign-board";
import { StorefrontOfferBoard } from "@/components/storefront/storefront-offer-board";
import { StatusBanner } from "@/components/feedback/status-banner";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import { sortProductsByOpportunity } from "@/features/catalog/merchandising";
import { summarizeCatalogPromotionLanes } from "@/features/catalog/promotion-lanes";
import type { PublicPageSummary } from "@/features/cms/types";
import { createStorefrontPaymentIntentAction } from "@/features/checkout/actions";
import type { PublicStorefrontOrderConfirmation } from "@/features/checkout/types";
import type {
  MemberInvoiceSummary,
  MemberOrderSummary,
  MyLoyaltyOverview,
} from "@/features/member-portal/types";
import { buildStorefrontOfferCards } from "@/features/storefront/storefront-campaigns";
import {
  formatResource,
  getCommerceResource,
  resolveApiStatusLabel,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { parseAddressJson, type ParsedAddress } from "@/lib/address-json";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { buildOrderPath } from "@/lib/entity-paths";
import { buildAppQueryPath, buildLocalizedAuthHref, localizeHref, sanitizeAppPath } from "@/lib/locale-routing";

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

function localizeConfirmationStatus(status: string | undefined, culture: string) {
  const normalized = (status ?? "").trim().toLowerCase();
  const english = culture.toLowerCase().startsWith("en");

  if (!normalized) {
    return english ? "Unavailable" : "Nicht verfuegbar";
  }

  const labels: Record<string, { de: string; en: string }> = {
    idle: { de: "Bereit", en: "Ready" },
    ok: { de: "Aktuell", en: "Current" },
    "order-placed": { de: "Bestellung platziert", en: "Order placed" },
    completed: { de: "Abgeschlossen", en: "Completed" },
    failed: { de: "Fehlgeschlagen", en: "Failed" },
    "missing-context": { de: "Zahlungskontext fehlt", en: "Payment context missing" },
    "not-found": { de: "Nicht gefunden", en: "Not found" },
    "http-error": { de: "Dienstfehler", en: "Service error" },
    "network-error": { de: "Netzwerkfehler", en: "Network error" },
    unavailable: { de: "Nicht verfuegbar", en: "Unavailable" },
  };

  const label = labels[normalized];
  return label ? (english ? label.en : label.de) : normalized;
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

  if (latestAttempt) {
    return latestAttempt.status;
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
  const localizedStatus = localizeConfirmationStatus(status, culture);
  const localizedMemberOrdersStatus =
    resolveApiStatusLabel(memberOrdersStatus, copy) ??
    localizeConfirmationStatus(memberOrdersStatus, culture);
  const localizedMemberInvoicesStatus =
    resolveApiStatusLabel(memberInvoicesStatus, copy) ??
    localizeConfirmationStatus(memberInvoicesStatus, culture);
  const localizedMemberLoyaltyStatus =
    resolveApiStatusLabel(memberLoyaltyStatus, copy) ??
    localizeConfirmationStatus(memberLoyaltyStatus, culture);
  const productsStatusLabel =
    resolveApiStatusLabel(productsStatus, copy) ??
    localizeConfirmationStatus(productsStatus, culture);

  if (!confirmation) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
        <div className="w-full rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title={copy.orderConfirmationUnavailableTitle}
            message={resolvedMessage ?? formatResource(copy.orderConfirmationUnavailableMessage, {
              status: localizedStatus,
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
  const recordedPaymentAmountMinor = getRecordedPaymentAmountMinor(confirmation);
  const remainingPaymentAmountMinor = Math.max(
    confirmation.grandTotalGrossMinor - recordedPaymentAmountMinor,
    0,
  );
  const paymentCoverageComplete = remainingPaymentAmountMinor === 0;
  const memberOrderDetailHref = sanitizeAppPath(
    buildOrderPath(confirmation.orderId),
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
  const localizedOrderStatus = localizeOrderStatus(confirmation.status, culture);
  const localizedPaymentStatus = localizePaymentStatus(
    displayedPaymentStatus,
    culture,
  );
  const localizedRouteStatus = localizedStatus;
  const localizedPaymentCompletionStatus = localizeConfirmationStatus(
    paymentCompletionStatus ?? "idle",
    culture,
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
  const visiblePaymentProviders = [
    ...new Set(
      confirmation.payments
        .map((payment) => payment.provider.trim())
        .filter((provider) => provider.length > 0),
    ),
  ];
  const purchasedNames = new Set(
    confirmation.lines.map((line) => line.name.trim().toLowerCase()),
  );
  const guestOfferBoard = sortProductsByOpportunity(
    products.filter((product) => !purchasedNames.has(product.name.trim().toLowerCase())),
  ).slice(0, 3);
  const guestOfferBoardCards = buildStorefrontOfferCards(guestOfferBoard, {
    labels: {
      heroOffer: copy.offerCampaignHeroLabel,
      valueOffer: copy.offerCampaignValueLabel,
      priceDrop: copy.offerCampaignPriceDropLabel,
      steadyPick: copy.offerCampaignSteadyLabel,
    },
    formatPrice: (product) =>
      formatMoney(product.priceMinor, product.currency, culture),
    describeWithSavings: (_, input) =>
      formatResource(copy.confirmationGuestOfferBoardOfferDescription, {
        campaignLabel: input.campaignLabel,
        savingsPercent: input.savingsPercent,
        price: input.price,
      }),
    describeWithoutSavings: (product) =>
      product.shortDescription ?? copy.confirmationGuestOfferBoardFallbackDescription,
    fallbackDescription: copy.confirmationGuestOfferBoardFallbackDescription,
  });
  const guestPromotionLaneCards = summarizeCatalogPromotionLanes(guestOfferBoard).map((entry) => {
    const laneLabel =
      entry.lane === "hero-offers"
        ? copy.storefrontWindowPromotionLaneHeroLabel
        : entry.lane === "value-offers"
          ? copy.storefrontWindowPromotionLaneValueLabel
          : entry.lane === "live-offers"
            ? copy.storefrontWindowPromotionLaneLiveOffersLabel
            : copy.storefrontWindowPromotionLaneBaseLabel;
    const href =
      entry.lane === "hero-offers"
        ? buildAppQueryPath("/catalog", { visibleState: "offers", visibleSort: "offers-first", savingsBand: "hero" })
        : entry.lane === "value-offers"
          ? buildAppQueryPath("/catalog", { visibleState: "offers", visibleSort: "offers-first", savingsBand: "value" })
          : entry.lane === "live-offers"
            ? buildAppQueryPath("/catalog", { visibleState: "offers", visibleSort: "savings-desc" })
            : buildAppQueryPath("/catalog", { visibleState: "base", visibleSort: "base-first" });

    return {
      id: `confirmation-guest-promotion-${entry.lane}`,
      label: copy.storefrontWindowPromotionLaneCardLabel,
      title: entry.anchorProduct
        ? formatResource(copy.storefrontWindowPromotionLaneTitle, {
            lane: laneLabel,
            product: entry.anchorProduct.name,
          })
        : formatResource(copy.storefrontWindowPromotionLaneFallbackTitle, {
            lane: laneLabel,
          }),
      description:
        entry.anchorProduct !== null
          ? formatResource(copy.storefrontWindowPromotionLaneDescription, {
              lane: laneLabel,
              count: entry.count,
              price: formatMoney(
                entry.anchorProduct.priceMinor,
                entry.anchorProduct.currency,
                culture,
              ),
            })
          : formatResource(copy.storefrontWindowPromotionLaneFallbackDescription, {
              lane: laneLabel,
            }),
      href,
      ctaLabel: copy.storefrontWindowPromotionLaneCta,
      meta: formatResource(copy.storefrontWindowPromotionLaneMeta, {
        count: entry.count,
      }),
    };
  });
  const sectionLinks = [
    { id: "confirmation-overview", label: copy.confirmationRouteSummaryTitle },
    { id: "confirmation-payment-window", label: copy.confirmationPaymentWindowTitle },
    { id: "confirmation-order", label: copy.summaryTitle },
    { id: "confirmation-composition", label: copy.confirmationCompositionJourneyTitle },
  ];

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

        <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
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
              orderStatus: localizedOrderStatus,
              paymentStatus: localizedPaymentStatus,
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
              status: localizedStatus,
            })}
          />
        )}

        <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
            {copy.confirmationRouteSummaryTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.confirmationRouteSummaryMessage, {
              status: localizedRouteStatus,
              paymentCompletionStatus: localizedPaymentCompletionStatus,
              paymentStatus: localizedPaymentStatus,
            })}
          </p>
        </div>

        <section className="sticky top-4 z-10 rounded-[1rem] border border-[var(--color-border-soft)] bg-[color:color-mix(in_srgb,var(--color-surface-panel)_92%,white_8%)] px-6 py-5 shadow-[var(--shadow-panel)] backdrop-blur">
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
          id="confirmation-overview"
          className="scroll-mt-28 grid gap-5 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)]"
        >
          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.confirmationCareTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {paymentNeedsAttention
                ? copy.confirmationCareAttentionMessage
                : copy.confirmationCareStableMessage}
            </p>
            <div className="mt-4 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.confirmationCarePaymentLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {paymentNeedsAttention
                    ? copy.confirmationCarePaymentPending
                    : copy.confirmationCarePaymentDone}
                </p>
              </div>
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.confirmationCareReferenceLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {confirmation.orderNumber}
                </p>
              </div>
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.confirmationCareStatusLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {localizedOrderStatus}
                </p>
              </div>
            </div>
          </section>

          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.confirmationNextWindowTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {hasMemberSession
                ? copy.confirmationNextWindowMemberMessage
                : copy.confirmationNextWindowGuestMessage}
            </p>
            <div className="mt-4 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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

        <section
          id="confirmation-payment-window"
          className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]"
        >
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
            {copy.confirmationPaymentWindowTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {copy.confirmationPaymentWindowMessage}
          </p>
          <div className="mt-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.confirmationPaymentWindowAttemptsLabel}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {formatResource(copy.confirmationPaymentWindowAttemptsValue, {
                  count: confirmation.payments.length,
                })}
              </p>
            </article>
            <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.confirmationPaymentWindowProviderLabel}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {visiblePaymentProviders.length === 0
                  ? copy.confirmationPaymentWindowProviderEmpty
                  : visiblePaymentProviders.length === 1
                    ? visiblePaymentProviders[0]
                    : formatResource(copy.confirmationPaymentWindowProviderValue, {
                        count: visiblePaymentProviders.length,
                      })}
              </p>
            </article>
            <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.confirmationPaymentWindowRouteLabel}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {paymentNeedsAttention
                  ? copy.confirmationPaymentWindowRoutePending
                  : copy.confirmationPaymentWindowRouteDone}
              </p>
            </article>
            <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.confirmationPaymentWindowAmountLabel}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {formatMoney(
                  confirmation.grandTotalGrossMinor,
                  confirmation.currency,
                  culture,
                )}
              </p>
            </article>
            <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.confirmationPaymentWindowRecordedLabel}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {formatMoney(
                  recordedPaymentAmountMinor,
                  confirmation.currency,
                  culture,
                )}
              </p>
            </article>
            <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.confirmationPaymentWindowRemainingLabel}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {formatMoney(
                  remainingPaymentAmountMinor,
                  confirmation.currency,
                  culture,
                )}
              </p>
            </article>
            <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.confirmationPaymentWindowCoverageLabel}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {paymentCoverageComplete
                  ? copy.confirmationPaymentWindowCoverageComplete
                  : copy.confirmationPaymentWindowCoverageOpen}
              </p>
            </article>
          </div>
        </section>

        <div
          id="confirmation-order"
          className="scroll-mt-28 grid gap-8 lg:grid-cols-[minmax(0,1.05fr)_360px]"
        >
          <div className="flex flex-col gap-6">
            <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
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

            <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.orderLinesTitle}
              </p>
              <div className="mt-5 flex flex-col gap-4">
                {confirmation.lines.map((line) => (
                  <article
                    key={line.id}
                    className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4"
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
            <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.nextStepsTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.nextStepsDescription}
              </p>
              <div className="mt-5 grid gap-3">
                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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

                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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

                <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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
              <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                  {copy.confirmationMemberWindowTitle}
                </p>
                <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {formatResource(copy.confirmationMemberWindowMessage, {
                    ordersStatus: localizedMemberOrdersStatus,
                    invoicesStatus: localizedMemberInvoicesStatus,
                    loyaltyStatus: localizedMemberLoyaltyStatus,
                  })}
                </p>
                <div className="mt-5 grid gap-3">
                  <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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
                  <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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
                  <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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
              <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                  {copy.confirmationGuestWindowTitle}
                </p>
                <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {formatResource(copy.confirmationGuestWindowMessage, {
                    returnPath: memberOrderDetailHref,
                  })}
                </p>
                <div className="mt-5 grid gap-3">
                  <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {copy.confirmationGuestReferenceLabel}
                    </p>
                    <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                      {formatResource(copy.confirmationGuestReferenceValue, {
                        orderNumber: confirmation.orderNumber,
                      })}
                    </p>
                  </article>
                  <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {copy.confirmationGuestReturnLabel}
                    </p>
                    <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                      {memberOrderDetailHref}
                    </p>
                  </article>
                  <article className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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
                        productCount: guestOfferBoardCards.length,
                      })}
                    </p>
                    <div className="mt-3">
                      <StorefrontOfferBoard
                        culture={culture}
                        cards={guestOfferBoardCards}
                        emptyMessage={copy.confirmationGuestOfferBoardEmptyMessage}
                        columnsClassName="grid-cols-1"
                      />
                    </div>
                    <div className="mt-4 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-4">
                      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                        {copy.storefrontWindowPromotionLaneSectionTitle}
                      </p>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {copy.storefrontWindowPromotionLaneSectionMessage}
                      </p>
                      <StorefrontCampaignBoard
                        culture={culture}
                        cards={guestPromotionLaneCards}
                        emptyMessage={formatResource(copy.storefrontWindowPromotionLaneEmptyMessage, {
                          productsStatus: productsStatusLabel,
                        })}
                      />
                    </div>
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

            <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.summaryTitle}
              </p>
              <div className="mt-5 space-y-3 text-sm text-[var(--color-text-secondary)]">
                <div className="flex items-center justify-between">
                  <span>{copy.statusLabel}</span>
                  <span>{localizedOrderStatus}</span>
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
                <div className="mt-6 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  <p className="font-semibold text-[var(--color-text-primary)]">{copy.shippingSnapshotTitle}</p>
                  <p>{confirmation.shippingMethodName ?? copy.methodUnavailable}</p>
                  <p>
                    {confirmation.shippingCarrier ?? copy.carrierUnavailable}
                    {confirmation.shippingService ? ` / ${confirmation.shippingService}` : ""}
                  </p>
                </div>
              )}
            </aside>

            <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.paymentsTitle}
              </p>
              <div className="mt-4 flex flex-col gap-3">
                {confirmation.payments.length > 0 ? (
                  confirmation.payments.map((payment) => (
                    <div
                      key={payment.id}
                      className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]"
                    >
                      <p className="font-semibold text-[var(--color-text-primary)]">
                        {payment.provider} - {localizePaymentStatus(payment.status, culture)}
                      </p>
                      <p>{formatMoney(payment.amountMinor, payment.currency, culture)}</p>
                      <p>{copy.createdLabel} {formatDateTime(payment.createdAtUtc, culture)}</p>
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

            <div id="confirmation-composition" className="scroll-mt-28">
              <ConfirmationContentCompositionWindow
                culture={culture}
                hasMemberSession={hasMemberSession}
                paymentNeedsAttention={paymentNeedsAttention}
                orderNumber={confirmation.orderNumber}
                orderGrossMinor={confirmation.grandTotalGrossMinor}
                currency={confirmation.currency}
                memberOrdersHref={memberOrdersHref}
                signInHref={signInHref}
                accountHref={localizeHref("/account", culture)}
                memberOrders={memberOrders}
                memberInvoices={memberInvoices}
                memberLoyaltyOverview={memberLoyaltyOverview}
                cmsPages={cmsPages}
                categories={categories}
                products={products}
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


