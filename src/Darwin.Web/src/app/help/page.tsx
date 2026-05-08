import { HelpPagesIndex } from "@/components/cms/help-pages-index";
import { getCmsIndexSeoMetadata } from "@/features/cms/server/get-cms-index-seo-metadata";
import { getCmsIndexPageContext } from "@/features/cms/server/get-cms-page-context";
import {
  readPositiveIntegerSearchParam,
  readSearchTextParam,
} from "@/features/checkout/helpers";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata({
  searchParams,
}: {
  searchParams?: Promise<{
    page?: string;
    search?: string;
  }>;
}) {
  const culture = await getRequestCulture();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const safePage = readPositiveIntegerSearchParam(resolvedSearchParams?.page);
  const search = readSearchTextParam(resolvedSearchParams?.search);
  const { metadata } = await getCmsIndexSeoMetadata(culture, safePage, search);
  return metadata;
}

type HelpRouteProps = {
  searchParams?: Promise<{
    page?: string;
    search?: string;
  }>;
};

export default async function HelpRoute({ searchParams }: HelpRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const culture = await getRequestCulture();
  const safePage = readPositiveIntegerSearchParam(resolvedSearchParams?.page);
  const searchQuery = readSearchTextParam(resolvedSearchParams?.search);
  const {
    browseContext,
    visibleWindow,
    matchingSetResult,
  } = await getCmsIndexPageContext(
    culture,
    safePage,
    searchQuery,
  );
  const { pagesResult } = browseContext;

  return (
    <HelpPagesIndex
      culture={culture}
      pages={visibleWindow.items}
      totalPages={visibleWindow.totalPages}
      currentPage={visibleWindow.currentPage}
      status={matchingSetResult?.status ?? pagesResult.status}
      searchQuery={searchQuery}
    />
  );
}
