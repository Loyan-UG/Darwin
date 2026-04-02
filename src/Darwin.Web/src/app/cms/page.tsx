import { CmsPagesIndex } from "@/components/cms/cms-pages-index";
import { getPublicCart } from "@/features/cart/api/public-cart";
import { getAnonymousCartId } from "@/features/cart/cookies";
import {
  getPublicCategories,
  getPublicProducts,
} from "@/features/catalog/api/public-catalog";
import { getPublishedPages } from "@/features/cms/api/public-cms";
import {
  readPositiveIntegerSearchParam,
  readSearchTextParam,
} from "@/features/checkout/helpers";
import { buildAppQueryPath } from "@/lib/locale-routing";
import { getSharedResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { buildSeoMetadata } from "@/lib/seo";

export async function generateMetadata({
  searchParams,
}: {
  searchParams?: Promise<{
    page?: string;
    visibleQuery?: string;
  }>;
}) {
  const culture = await getRequestCulture();
  const shared = getSharedResource(culture);
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const safePage = readPositiveIntegerSearchParam(resolvedSearchParams?.page);
  const visibleQuery = readSearchTextParam(resolvedSearchParams?.visibleQuery);

  return buildSeoMetadata({
    culture,
    title: shared.cmsIndexMetaTitle,
    description: shared.cmsIndexMetaDescription,
    path: buildAppQueryPath("/cms", {
      page: safePage > 1 ? safePage : undefined,
      visibleQuery,
    }),
    noIndex: safePage > 1 || Boolean(visibleQuery),
    allowLanguageAlternates: safePage === 1 && !visibleQuery,
  });
}

type CmsIndexRouteProps = {
  searchParams?: Promise<{
    page?: string;
    visibleQuery?: string;
  }>;
};

export default async function CmsIndexRoute({
  searchParams,
}: CmsIndexRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const culture = await getRequestCulture();
  const safePage = readPositiveIntegerSearchParam(resolvedSearchParams?.page);
  const visibleQuery = readSearchTextParam(resolvedSearchParams?.visibleQuery);
  const anonymousCartId = await getAnonymousCartId();
  const [pagesResult, categoriesResult, productsResult, cartResult] = await Promise.all([
    getPublishedPages({
      page: safePage,
      pageSize: 12,
      culture,
    }),
    getPublicCategories(culture),
    getPublicProducts({
      page: 1,
      pageSize: 3,
      culture,
    }),
    anonymousCartId
      ? getPublicCart(anonymousCartId)
      : Promise.resolve({ data: null, status: "not-found" as const }),
  ]);
  const visiblePages = visibleQuery
    ? (pagesResult.data?.items ?? []).filter((page) => {
        const haystack = `${page.title} ${page.slug} ${page.metaTitle ?? ""} ${page.metaDescription ?? ""}`.toLowerCase();
        return haystack.includes(visibleQuery.toLowerCase());
      })
    : (pagesResult.data?.items ?? []);

  return (
    <CmsPagesIndex
      culture={culture}
      pages={visiblePages}
      loadedPageCount={pagesResult.data?.items.length ?? 0}
      totalItems={pagesResult.data?.total ?? 0}
      pageSize={pagesResult.data?.request.pageSize ?? 12}
      totalPages={Math.max(
        1,
        Math.ceil(
          (pagesResult.data?.total ?? 0) /
            (pagesResult.data?.request.pageSize ?? 12),
        ),
      )}
      currentPage={safePage}
      status={pagesResult.status}
      visibleQuery={visibleQuery}
      categories={categoriesResult.data?.items.slice(0, 3) ?? []}
      categoriesStatus={categoriesResult.status}
      products={productsResult.data?.items ?? []}
      productsStatus={productsResult.status}
      cartSummary={
        cartResult.data
          ? {
              status: cartResult.status,
              itemCount: cartResult.data.items.length,
              currency: cartResult.data.currency,
              grandTotalGrossMinor: cartResult.data.grandTotalGrossMinor,
            }
          : null
      }
    />
  );
}
