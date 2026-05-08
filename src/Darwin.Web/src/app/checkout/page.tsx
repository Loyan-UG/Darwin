import { CheckoutPage } from "@/components/checkout/checkout-page";
import { createPublicCheckoutIntent } from "@/features/checkout/api/public-checkout";
import { getCheckoutPageContext } from "@/features/checkout/server/get-commerce-page-context";
import { getCheckoutSeoMetadata } from "@/features/checkout/server/get-commerce-seo-metadata";
import {
  hasCheckoutDraftValues,
  isCheckoutAddressComplete,
  mergeCheckoutDraft,
  readCheckoutDraftFromSearchParams,
  readSingleSearchParam,
  toCheckoutDraftFromMemberAddress,
  toCheckoutDraftFromMemberProfile,
  toCheckoutAddress,
} from "@/features/checkout/helpers";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const { metadata } = await getCheckoutSeoMetadata(culture);
  return metadata;
}

type CheckoutRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

export default async function CheckoutRoute({ searchParams }: CheckoutRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const culture = await getRequestCulture();
  const { routeContext } = await getCheckoutPageContext(culture);
  const {
    model,
    memberSession,
    identityContext,
  } = routeContext;
  const requestedDraft = readCheckoutDraftFromSearchParams(resolvedSearchParams);
  const checkoutError = readSingleSearchParam(resolvedSearchParams?.checkoutError);
  const selectedMemberAddressId = readSingleSearchParam(
    resolvedSearchParams?.memberAddressId,
  );
  const memberAddresses = identityContext?.addressesResult.data ?? [];
  const memberProfile = identityContext?.profileResult.data ?? null;
  const preferredMemberAddress =
    memberAddresses.find((address) => address.id === selectedMemberAddressId) ??
    memberAddresses.find((address) => address.isDefaultShipping) ??
    memberAddresses.find((address) => address.isDefaultBilling) ??
    memberAddresses[0] ??
    null;
  const draft = !hasCheckoutDraftValues(requestedDraft)
    ? preferredMemberAddress
      ? toCheckoutDraftFromMemberAddress(preferredMemberAddress)
      : memberProfile
        ? toCheckoutDraftFromMemberProfile(memberProfile)
        : requestedDraft
    : memberProfile
      ? mergeCheckoutDraft(
          requestedDraft,
          toCheckoutDraftFromMemberProfile(memberProfile),
        )
      : requestedDraft;
  const effectiveSelectedMemberAddressId =
    selectedMemberAddressId || preferredMemberAddress?.id;

  let intent = null;
  let intentStatus = "idle";
  let intentMessage: string | undefined;

  if (model.cart && isCheckoutAddressComplete(draft)) {
    const intentResult = await createPublicCheckoutIntent({
      cartId: model.cart.cartId,
      shippingAddress: toCheckoutAddress(draft),
      selectedShippingMethodId: draft.selectedShippingMethodId || undefined,
    });

    intent = intentResult.data;
    intentStatus = intentResult.status;
    intentMessage = intentResult.message;
  }

  return (
    <CheckoutPage
      culture={culture}
      model={model}
      draft={draft}
      intent={intent}
      intentStatus={intentStatus}
      intentMessage={intentMessage}
      checkoutError={checkoutError}
      memberAddresses={memberAddresses}
      profilePrefillActive={!preferredMemberAddress && Boolean(memberProfile)}
      selectedMemberAddressId={effectiveSelectedMemberAddressId}
      hasMemberSession={Boolean(memberSession)}
    />
  );
}
