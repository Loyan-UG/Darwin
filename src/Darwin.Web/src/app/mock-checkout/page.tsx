import { MockCheckoutPage } from "@/components/checkout/mock-checkout-page";
import { getMockCheckoutSeoMetadata } from "@/features/checkout/server/get-commerce-seo-metadata";
import { getCommerceResource } from "@/localization";
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
    return url.protocol === "http:" || url.protocol === "https:" ? url : null;
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
  const { metadata } = await getMockCheckoutSeoMetadata(culture);
  return metadata;
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
    getCommerceResource(culture).mockCheckoutFailureReason,
  );
  const copy = getCommerceResource(culture);

  return (
    <MockCheckoutPage
      culture={culture}
      orderId={orderId || copy.mockCheckoutMissingOrderId}
      paymentId={paymentId || copy.mockCheckoutMissingPaymentId}
      provider={provider}
      sessionToken={sessionToken || copy.mockCheckoutMissingSessionToken}
      returnUrl={tryParseAbsoluteUrl(returnUrl)?.toString() ?? null}
      cancelUrl={tryParseAbsoluteUrl(cancelUrl)?.toString() ?? null}
      successUrl={successUrl}
      failureUrl={failureUrl}
      cancelActionUrl={cancelActionUrl}
      title={copy.mockCheckoutPageTitle}
      description={copy.mockCheckoutPageDescription}
    />
  );
}

