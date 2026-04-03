import { CheckoutPage } from "@/components/checkout/checkout-page";
import { getCartViewModel } from "@/features/cart/server/get-cart-view-model";
import { createPublicCheckoutIntent } from "@/features/checkout/api/public-checkout";
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
import { getMemberSession } from "@/features/member-session/cookies";
import {
  getMemberCommerceSummaryContext,
  getMemberIdentityContext,
} from "@/features/member-portal/server/get-member-summary-context";
import { getStorefrontContinuationContext } from "@/features/storefront/server/get-storefront-continuation-context";
import { getCommerceResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { observeAsyncOperation } from "@/lib/route-observability";
import { buildNoIndexMetadata } from "@/lib/seo";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const copy = getCommerceResource(culture);

  return buildNoIndexMetadata(
    culture,
    copy.checkoutMetaTitle,
    copy.checkoutMetaDescription,
    "/checkout",
  );
}

type CheckoutRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

export default async function CheckoutRoute({ searchParams }: CheckoutRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const culture = await getRequestCulture();
  const [model, memberSession, storefrontContext] = await observeAsyncOperation(
    {
      area: "checkout",
      operation: "load-route",
      thresholdMs: 350,
    },
    () =>
      Promise.all([
        getCartViewModel(),
        getMemberSession(),
        getStorefrontContinuationContext(culture),
      ]),
  );
  const requestedDraft = readCheckoutDraftFromSearchParams(resolvedSearchParams);
  const checkoutError = readSingleSearchParam(resolvedSearchParams?.checkoutError);
  const selectedMemberAddressId = readSingleSearchParam(
    resolvedSearchParams?.memberAddressId,
  );
  const [identityContext, commerceSummaryContext] = memberSession
    ? await Promise.all([
        getMemberIdentityContext(),
        getMemberCommerceSummaryContext(),
      ])
    : [null, null];
  const memberAddresses = identityContext?.addressesResult.data ?? [];
  const memberProfile = identityContext?.profileResult.data ?? null;
  const memberPreferences = identityContext?.preferencesResult.data ?? null;
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
      memberAddressesStatus={identityContext?.addressesResult.status ?? "idle"}
      memberProfile={memberProfile}
      memberProfileStatus={identityContext?.profileResult.status ?? "idle"}
      memberPreferences={memberPreferences}
      memberPreferencesStatus={identityContext?.preferencesResult.status ?? "idle"}
      memberInvoices={commerceSummaryContext?.invoicesResult.data?.items ?? []}
      memberInvoicesStatus={commerceSummaryContext?.invoicesResult.status ?? "idle"}
      profilePrefillActive={!preferredMemberAddress && Boolean(memberProfile)}
      selectedMemberAddressId={effectiveSelectedMemberAddressId}
      hasMemberSession={Boolean(memberSession)}
      cmsPages={storefrontContext.cmsPages}
      cmsPagesStatus={storefrontContext.cmsPagesStatus}
      categories={storefrontContext.categories}
      categoriesStatus={storefrontContext.categoriesStatus}
      products={storefrontContext.products}
      productsStatus={storefrontContext.productsStatus}
    />
  );
}
