import { CmsPagesIndex } from "@/components/cms/cms-pages-index";
import { getPublishedPages } from "@/features/cms/api/public-cms";

export const metadata = {
  title: "CMS",
};

type CmsIndexRouteProps = {
  searchParams?: Promise<{
    page?: string;
  }>;
};

export default async function CmsIndexRoute({
  searchParams,
}: CmsIndexRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const page = Number(resolvedSearchParams?.page ?? "1");
  const safePage = Number.isFinite(page) && page > 0 ? page : 1;
  const pagesResult = await getPublishedPages({
    page: safePage,
    pageSize: 12,
  });

  return (
    <CmsPagesIndex
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
