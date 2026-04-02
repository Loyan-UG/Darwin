import { CmsPagesIndex } from "@/components/cms/cms-pages-index";
import { getPublishedPages } from "@/features/cms/api/public-cms";
import {
  readPositiveIntegerSearchParam,
  readSearchTextParam,
} from "@/features/checkout/helpers";
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
    path:
      safePage > 1 || visibleQuery
        ? `/cms?${new URLSearchParams({
            ...(safePage > 1 ? { page: String(safePage) } : {}),
            ...(visibleQuery ? { visibleQuery } : {}),
          }).toString()}`
        : "/cms",
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
  const pagesResult = await getPublishedPages({
    page: safePage,
    pageSize: 12,
    culture,
  });
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
    />
  );
}
