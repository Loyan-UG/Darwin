import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { getPublicAuthStorefrontContext } from "@/features/account/server/get-public-auth-storefront-context";
import { readCartDisplaySnapshots } from "@/features/cart/cookies";
import {
  readAllowedSearchParam,
  readPositiveIntegerSearchParam,
  readSearchTextParam,
} from "@/features/checkout/helpers";
import { InvoicesPage } from "@/components/member/invoices-page";
import { getCurrentMemberInvoices } from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { getStorefrontContinuationContext } from "@/features/storefront/server/get-storefront-continuation-context";
import { getMemberResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { observeAsyncOperation } from "@/lib/route-observability";
import { buildNoIndexMetadata } from "@/lib/seo";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);

  return buildNoIndexMetadata(
    culture,
    copy.invoicesMetaTitle,
    undefined,
    "/invoices",
  );
}

type InvoicesRouteProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

export default async function InvoicesRoute({
  searchParams,
}: InvoicesRouteProps) {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);
  const session = await getMemberSession();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const safePage = readPositiveIntegerSearchParam(resolvedSearchParams?.page);
  const visibleQuery = readSearchTextParam(resolvedSearchParams?.visibleQuery);
  const visibleState = readAllowedSearchParam(
    resolvedSearchParams?.visibleState,
    ["all", "outstanding", "settled"] as const,
  );

  if (!session) {
    const storefrontContext = await getPublicAuthStorefrontContext(culture);
    return (
      <MemberAuthRequired
        culture={culture}
        title={copy.invoicesAuthRequiredTitle}
        message={copy.invoicesAuthRequiredMessage}
        returnPath="/invoices"
        cmsPages={storefrontContext.cmsPages}
        cmsPagesStatus={storefrontContext.cmsPagesStatus}
        categories={storefrontContext.categories}
        categoriesStatus={storefrontContext.categoriesStatus}
        products={storefrontContext.products}
        productsStatus={storefrontContext.productsStatus}
        storefrontCart={storefrontContext.storefrontCart}
        storefrontCartStatus={storefrontContext.storefrontCartStatus}
      />
    );
  }

  const [invoicesResult, storefrontContext] = await observeAsyncOperation(
    {
      area: "invoices",
      operation: "load-route",
      thresholdMs: 325,
    },
    () =>
      Promise.all([
        getCurrentMemberInvoices({
          page: safePage,
          pageSize: 12,
        }),
        getStorefrontContinuationContext(culture),
      ]),
  );
  const cartLinkedProductSlugs = (await readCartDisplaySnapshots())
    .map((snapshot) => {
      const match = snapshot.href.match(/\/catalog\/([^/?#]+)/i);
      return match?.[1] ?? null;
    })
    .filter((slug): slug is string => Boolean(slug));

  return (
    <InvoicesPage
      culture={culture}
      invoices={invoicesResult.data?.items ?? []}
      status={invoicesResult.status}
      currentPage={safePage}
      totalPages={Math.max(1, Math.ceil((invoicesResult.data?.total ?? 0) / (invoicesResult.data?.request.pageSize ?? 12)))}
      visibleQuery={visibleQuery}
      visibleState={visibleState ?? "all"}
      cmsPages={storefrontContext.cmsPages}
      cmsPagesStatus={storefrontContext.cmsPagesStatus}
      categories={storefrontContext.categories}
      categoriesStatus={storefrontContext.categoriesStatus}
      products={storefrontContext.products}
      productsStatus={storefrontContext.productsStatus}
      cartLinkedProductSlugs={cartLinkedProductSlugs}
    />
  );
}
