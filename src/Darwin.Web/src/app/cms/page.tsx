import { CmsPagesIndex } from "@/components/cms/cms-pages-index";
import { getPublishedPages } from "@/features/cms/api/public-cms";
import { getSharedResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { buildSeoMetadata } from "@/lib/seo";

export async function generateMetadata({
  searchParams,
}: {
  searchParams?: Promise<{
    page?: string;
  }>;
}) {
  const culture = await getRequestCulture();
  const shared = getSharedResource(culture);
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const page = Number(resolvedSearchParams?.page ?? "1");
  const safePage = Number.isFinite(page) && page > 0 ? page : 1;

  return buildSeoMetadata({
    culture,
    title: shared.cmsIndexMetaTitle,
    description: shared.cmsIndexMetaDescription,
    path: safePage > 1 ? `/cms?page=${safePage}` : "/cms",
    noIndex: safePage > 1,
    allowLanguageAlternates: safePage === 1,
  });
}

type CmsIndexRouteProps = {
  searchParams?: Promise<{
    page?: string;
  }>;
};

export default async function CmsIndexRoute({
  searchParams,
}: CmsIndexRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const culture = await getRequestCulture();
  const page = Number(resolvedSearchParams?.page ?? "1");
  const safePage = Number.isFinite(page) && page > 0 ? page : 1;
  const pagesResult = await getPublishedPages({
    page: safePage,
    pageSize: 12,
  });

  return (
    <CmsPagesIndex
      culture={culture}
      pages={pagesResult.data?.items ?? []}
      totalPages={Math.max(
        1,
        Math.ceil(
          (pagesResult.data?.total ?? 0) /
            (pagesResult.data?.request.pageSize ?? 12),
        ),
      )}
      currentPage={safePage}
      status={pagesResult.status}
    />
  );
}
