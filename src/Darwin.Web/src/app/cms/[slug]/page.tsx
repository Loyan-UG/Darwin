import { notFound } from "next/navigation";
import { CmsPageDetail } from "@/components/cms/cms-page-detail";
import {
  readCmsMetadataFocus,
  readCmsVisibleSort,
  readCmsVisibleState,
} from "@/features/cms/discovery";
import { getCmsDetailPageContext } from "@/features/cms/server/get-cms-page-context";
import { getCmsSeoMetadata } from "@/features/cms/server/get-cms-seo-metadata";
import { readSearchTextParam } from "@/features/checkout/helpers";
import { getRequestCulture } from "@/lib/request-culture";

type CmsPageProps = {
  params: Promise<{
    slug: string;
  }>;
  searchParams?: Promise<{
    visibleQuery?: string;
    visibleState?: string;
    visibleSort?: string;
    metadataFocus?: string;
  }>;
};

export async function generateMetadata({ params }: CmsPageProps) {
  const culture = await getRequestCulture();
  const { slug } = await params;
  const { metadata } = await getCmsSeoMetadata(culture, slug);
  return metadata;
}

export default async function CmsPage({ params, searchParams }: CmsPageProps) {
  const culture = await getRequestCulture();
  const { slug } = await params;
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const visibleQuery = readSearchTextParam(resolvedSearchParams?.visibleQuery);
  const visibleState = readCmsVisibleState(resolvedSearchParams?.visibleState);
  const visibleSort = readCmsVisibleSort(resolvedSearchParams?.visibleSort);
  const metadataFocus = readCmsMetadataFocus(resolvedSearchParams?.metadataFocus);
  const { detailContext, continuationSlice } = await getCmsDetailPageContext(
    culture,
    slug,
    visibleQuery,
    visibleState,
    visibleSort,
    metadataFocus,
  );
  const { pageResult, relatedPagesResult, relatedPages } = detailContext;
  const page = pageResult.data;

  if (!page && pageResult.status === "not-found") {
    notFound();
  }

  return (
    <CmsPageDetail
      culture={culture}
      page={page}
      status={pageResult.status}
      message={pageResult.message}
      reviewWindow={{
        visibleQuery,
        visibleState,
        visibleSort,
        metadataFocus,
      }}
      relatedPages={relatedPages}
      relatedStatus={relatedPagesResult.status}
      categories={continuationSlice.categories}
      categoriesStatus={continuationSlice.categoriesStatus}
      products={continuationSlice.products}
      productsStatus={continuationSlice.productsStatus}
      cartSummary={continuationSlice.cartSummary}
    />
  );
}

