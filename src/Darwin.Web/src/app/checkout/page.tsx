import { CheckoutPage } from "@/components/checkout/checkout-page";
import { getCartViewModel } from "@/features/cart/server/get-cart-view-model";
import { getPublicCategories, getPublicProducts } from "@/features/catalog/api/public-catalog";
import { getPublishedPages } from "@/features/cms/api/public-cms";
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
import {
  getCurrentMemberAddresses,
  getCurrentMemberInvoices,
  getCurrentMemberPreferences,
  getCurrentMemberProfile,
} from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { getCommerceResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
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
  const [model, memberSession] = await Promise.all([
    getCartViewModel(),
    getMemberSession(),
  ]);
  const [cmsPagesResult, categoriesResult, productsResult] = await Promise.all([
    getPublishedPages({
      page: 1,
      pageSize: 3,
      culture,
    }),
    getPublicCategories(culture),
    getPublicProducts({
      page: 1,
      pageSize: 3,
      culture,
    }),
  ]);
  const requestedDraft = readCheckoutDraftFromSearchParams(resolvedSearchParams);
  const checkoutError = readSingleSearchParam(resolvedSearchParams?.checkoutError);
  const selectedMemberAddressId = readSingleSearchParam(
    resolvedSearchParams?.memberAddressId,
  );
  const [
    memberAddressesResult,
    memberProfileResult,
    memberPreferencesResult,
    memberInvoicesResult,
  ] = memberSession
    ? await Promise.all([
        getCurrentMemberAddresses(),
        getCurrentMemberProfile(),
        getCurrentMemberPreferences(),
        getCurrentMemberInvoices({
          page: 1,
          pageSize: 3,
        }),
      ])
    : [null, null, null, null];
  const memberAddresses = memberAddressesResult?.data ?? [];
  const memberProfile = memberProfileResult?.data ?? null;
  const memberPreferences = memberPreferencesResult?.data ?? null;
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
      memberAddressesStatus={memberAddressesResult?.status ?? "idle"}
      memberProfile={memberProfile}
      memberProfileStatus={memberProfileResult?.status ?? "idle"}
      memberPreferences={memberPreferences}
      memberPreferencesStatus={memberPreferencesResult?.status ?? "idle"}
      memberInvoices={memberInvoicesResult?.data?.items ?? []}
      memberInvoicesStatus={memberInvoicesResult?.status ?? "idle"}
      profilePrefillActive={!preferredMemberAddress && Boolean(memberProfile)}
      selectedMemberAddressId={effectiveSelectedMemberAddressId}
      hasMemberSession={Boolean(memberSession)}
      cmsPages={cmsPagesResult.data?.items ?? []}
      cmsPagesStatus={cmsPagesResult.status}
      categories={categoriesResult.data?.items.slice(0, 3) ?? []}
      categoriesStatus={categoriesResult.status}
      products={productsResult.data?.items ?? []}
      productsStatus={productsResult.status}
    />
  );
}
