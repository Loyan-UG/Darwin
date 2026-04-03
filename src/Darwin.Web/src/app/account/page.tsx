import { AccountHubPage } from "@/components/account/account-hub-page";
import { MemberDashboardPage } from "@/components/account/member-dashboard-page";
import { getPublicCategories, getPublicProducts } from "@/features/catalog/api/public-catalog";
import { getPublicCart } from "@/features/cart/api/public-cart";
import { getAnonymousCartId } from "@/features/cart/cookies";
import { getPublishedPages } from "@/features/cms/api/public-cms";
import { getPublicAuthStorefrontContext } from "@/features/account/server/get-public-auth-storefront-context";
import {
  getCurrentMemberAddresses,
  getCurrentMemberCustomerContext,
  getCurrentMemberInvoices,
  getCurrentMemberLoyaltyBusinesses,
  getCurrentMemberLoyaltyOverview,
  getCurrentMemberOrders,
  getCurrentMemberPreferences,
  getCurrentMemberProfile,
} from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { sanitizeAppPath } from "@/lib/locale-routing";
import { getRequestCulture } from "@/lib/request-culture";
import { buildNoIndexMetadata } from "@/lib/seo";
import { getMemberResource } from "@/localization";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);

  return buildNoIndexMetadata(culture, copy.accountMetaTitle, undefined, "/account");
}

type AccountPageProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function AccountPage({ searchParams }: AccountPageProps) {
  const culture = await getRequestCulture();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const session = await getMemberSession();
  if (!session) {
    const storefrontContext = await getPublicAuthStorefrontContext(culture);

    return (
      <AccountHubPage
        culture={culture}
        cmsPages={storefrontContext.cmsPages}
        cmsPagesStatus={storefrontContext.cmsPagesStatus}
        categories={storefrontContext.categories}
        categoriesStatus={storefrontContext.categoriesStatus}
        products={storefrontContext.products}
        productsStatus={storefrontContext.productsStatus}
        storefrontCart={storefrontContext.storefrontCart}
        storefrontCartStatus={storefrontContext.storefrontCartStatus}
        returnPath={sanitizeAppPath(
          readSearchParam(resolvedSearchParams?.returnPath),
          "/account",
        )}
      />
    );
  }
  const anonymousCartId = await getAnonymousCartId();

  const [
    profileResult,
    preferencesResult,
    customerContextResult,
    addressesResult,
    ordersResult,
    invoicesResult,
    loyaltyOverviewResult,
    loyaltyBusinessesResult,
    storefrontCartResult,
    cmsPagesResult,
    categoriesResult,
    productsResult,
  ] = await Promise.all([
    getCurrentMemberProfile(),
    getCurrentMemberPreferences(),
    getCurrentMemberCustomerContext(),
    getCurrentMemberAddresses(),
    getCurrentMemberOrders({ page: 1, pageSize: 3 }),
    getCurrentMemberInvoices({ page: 1, pageSize: 3 }),
    getCurrentMemberLoyaltyOverview(),
    getCurrentMemberLoyaltyBusinesses({ page: 1, pageSize: 3 }),
    anonymousCartId
      ? getPublicCart(anonymousCartId)
      : Promise.resolve({ data: null, status: "not-found" as const }),
    getPublishedPages({ page: 1, pageSize: 2, culture }),
    getPublicCategories(culture),
    getPublicProducts({ page: 1, pageSize: 3, culture }),
  ]);

  return (
    <MemberDashboardPage
      culture={culture}
      session={session}
      profile={profileResult.data}
      profileStatus={profileResult.status}
      preferences={preferencesResult.data}
      preferencesStatus={preferencesResult.status}
      customerContext={customerContextResult.data}
      customerContextStatus={customerContextResult.status}
      addresses={addressesResult.data ?? []}
      addressesStatus={addressesResult.status}
      recentOrders={ordersResult.data?.items ?? []}
      recentOrdersStatus={ordersResult.status}
      recentInvoices={invoicesResult.data?.items ?? []}
      recentInvoicesStatus={invoicesResult.status}
      loyaltyOverview={loyaltyOverviewResult.data}
      loyaltyOverviewStatus={loyaltyOverviewResult.status}
      loyaltyBusinesses={loyaltyBusinessesResult.data?.items ?? []}
      loyaltyBusinessesStatus={loyaltyBusinessesResult.status}
      storefrontCart={storefrontCartResult.data}
      storefrontCartStatus={storefrontCartResult.status}
      cmsPages={cmsPagesResult.data?.items ?? []}
      cmsPagesStatus={cmsPagesResult.status}
      categories={categoriesResult.data?.items.slice(0, 3) ?? []}
      categoriesStatus={categoriesResult.status}
      products={productsResult.data?.items ?? []}
      productsStatus={productsResult.status}
    />
  );
}
