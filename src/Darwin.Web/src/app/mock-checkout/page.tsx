import { MockCheckoutPage } from "@/components/checkout/mock-checkout-page";
import { getRequestCulture } from "@/lib/request-culture";

export function readSearchValue(
  value: string | string[] | undefined,
  maxLength = 512,
) {
  const raw = Array.isArray(value) ? value[0] : value;
  if (!raw) {
    return "";
  }

  return raw.trim().slice(0, maxLength);
}

export function tryParseAbsoluteUrl(value: string) {
  if (!value) {
    return null;
  }

  try {
    const url = new URL(value);
    return url;
  } catch {
    return null;
  }
}

export function toFinalizeUrl(value: string) {
  const url = tryParseAbsoluteUrl(value);
  if (!url) {
    return null;
  }

  url.pathname = `${url.pathname.replace(/\/$/, "")}/finalize`;
  return url;
}

export function buildOutcomeUrl(
  target: string,
  providerReference: string,
  outcome: "Succeeded" | "Cancelled" | "Failed",
  failureReason?: string,
) {
  const url = toFinalizeUrl(target);
  if (!url) {
    return null;
  }

  url.searchParams.set("providerReference", providerReference);
  url.searchParams.set("outcome", outcome);

  if (outcome === "Cancelled") {
    url.searchParams.set("cancelled", "true");
  }

  if (failureReason) {
    url.searchParams.set("failureReason", failureReason);
  }

  return url.toString();
}

export async function generateMetadata() {
  const culture = await getRequestCulture();

  return {
    title: culture === "de-DE" ? "Mock Checkout" : "Mock checkout",
    description:
      culture === "de-DE"
        ? "Lokale Hosted-Checkout-Simulation fuer Darwin-Web-Storefront-Flows."
        : "Local hosted-checkout simulation for Darwin storefront flows.",
  };
}

type MockCheckoutRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

export default async function MockCheckoutRoute({
  searchParams,
}: MockCheckoutRouteProps) {
  const culture = await getRequestCulture();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;

  const orderId = readSearchValue(resolvedSearchParams?.orderId, 64);
  const paymentId = readSearchValue(resolvedSearchParams?.paymentId, 64);
  const provider = readSearchValue(resolvedSearchParams?.provider, 128) || "DarwinCheckout";
  const sessionToken = readSearchValue(resolvedSearchParams?.sessionToken, 256);
  const returnUrl = readSearchValue(resolvedSearchParams?.returnUrl, 2048);
  const cancelUrl = readSearchValue(resolvedSearchParams?.cancelUrl, 2048);

  const successUrl = buildOutcomeUrl(returnUrl, sessionToken, "Succeeded");
  const cancelActionUrl = buildOutcomeUrl(cancelUrl, sessionToken, "Cancelled");
  const failureUrl = buildOutcomeUrl(
    returnUrl,
    sessionToken,
    "Failed",
    "Mock checkout marked the payment as failed.",
  );

  return (
    <MockCheckoutPage
      culture={culture}
      orderId={orderId || "missing-order-id"}
      paymentId={paymentId || "missing-payment-id"}
      provider={provider}
      sessionToken={sessionToken || "missing-session-token"}
      returnUrl={tryParseAbsoluteUrl(returnUrl)?.toString() ?? null}
      cancelUrl={tryParseAbsoluteUrl(cancelUrl)?.toString() ?? null}
      successUrl={successUrl}
      failureUrl={failureUrl}
      cancelActionUrl={cancelActionUrl}
      title={
        culture === "de-DE"
          ? "Lokaler Hosted Checkout"
          : "Local hosted checkout"
      }
      description={
        culture === "de-DE"
          ? "Diese Entwicklungsroute simuliert den PSP-Handoff fuer den Storefront-Checkout und fuehrt mit einem expliziten Erfolg-, Abbruch- oder Fehlerausgang in die Bestaetigungs-Reconciliation zurueck."
          : "This development route simulates the PSP handoff for storefront checkout and routes back into confirmation reconciliation with explicit success, cancellation, or failure outcomes."
      }
    />
  );
}

