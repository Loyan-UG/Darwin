import { notFound, redirect } from "next/navigation";
import { CmsPageDetail } from "@/components/cms/cms-page-detail";
import {
  readCmsMetadataFocus,
  readCmsVisibleSort,
  readCmsVisibleState,
} from "@/features/cms/discovery";
import { getCmsDetailPageContext } from "@/features/cms/server/get-cms-page-context";
import { getCmsSeoMetadata } from "@/features/cms/server/get-cms-seo-metadata";
import { readSearchTextParam } from "@/features/checkout/helpers";
import {
  INFERRED_CULTURE_SEARCH_PARAM,
  localizeHref,
} from "@/lib/locale-routing";
import { buildCmsPagePath } from "@/lib/entity-paths";
import { getRequestCulture, getSupportedCulturesAsync } from "@/lib/request-culture";

type CmsSearchParams = {
  visibleQuery?: string;
  visibleState?: string;
  visibleSort?: string;
  metadataFocus?: string;
};

type CmsPageProps = {
  params: Promise<{
    slug: string;
  }>;
  searchParams?: Promise<CmsSearchParams>;
};

function appendSearchParams(
  href: string,
  searchParams: CmsSearchParams | undefined,
  extraParams?: Record<string, string | undefined>,
) {
  const params = new URLSearchParams();

  for (const [key, value] of Object.entries(searchParams ?? {})) {
    if (value) {
      params.set(key, value);
    }
  }

  for (const [key, value] of Object.entries(extraParams ?? {})) {
    if (value) {
      params.set(key, value);
    }
  }

  const query = params.toString();
  return query ? `${href}?${query}` : href;
}

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
    for (const alternateCulture of await getSupportedCulturesAsync()) {
      if (alternateCulture === culture) {
        continue;
      }

      const alternateContext = await getCmsDetailPageContext(
        alternateCulture,
        slug,
        visibleQuery,
        visibleState,
        visibleSort,
        metadataFocus,
      );

      const alternatePage = alternateContext.detailContext.pageResult.data;
      if (alternatePage) {
        redirect(
          appendSearchParams(buildCmsPagePath(slug), resolvedSearchParams, {
            culture: alternateCulture,
            [INFERRED_CULTURE_SEARCH_PARAM]: "1",
          }),
        );
      }
    }

    notFound();
  }

  if (page?.slug && page.slug !== slug) {
    redirect(
      appendSearchParams(localizeHref(buildCmsPagePath(page.slug), culture), resolvedSearchParams),
    );
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
