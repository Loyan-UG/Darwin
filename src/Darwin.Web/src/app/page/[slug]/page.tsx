import { notFound, redirect } from "next/navigation";
import { HelpPageDetail } from "@/components/cms/help-page-detail";
import { getCmsDetailPageContext } from "@/features/cms/server/get-cms-page-context";
import { getCmsSeoMetadata } from "@/features/cms/server/get-cms-seo-metadata";
import {
  INFERRED_CULTURE_SEARCH_PARAM,
  localizeHref,
} from "@/lib/locale-routing";
import { buildCmsPagePath } from "@/lib/entity-paths";
import { getRequestCulture, getSupportedCulturesAsync } from "@/lib/request-culture";

type HelpSearchParams = Record<string, string | undefined>;

type HelpPageProps = {
  params: Promise<{
    slug: string;
  }>;
};

function appendSearchParams(
  href: string,
  searchParams: HelpSearchParams | undefined,
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

export async function generateMetadata({ params }: HelpPageProps) {
  const culture = await getRequestCulture();
  const { slug } = await params;
  const { metadata } = await getCmsSeoMetadata(culture, slug);
  return metadata;
}

export default async function HelpPage({ params }: HelpPageProps) {
  const culture = await getRequestCulture();
  const { slug } = await params;
  const { detailContext } = await getCmsDetailPageContext(
    culture,
    slug,
  );
  const { pageResult, relatedPages } = detailContext;
  const page = pageResult.data;

  if (!page && pageResult.status === "not-found") {
    for (const alternateCulture of await getSupportedCulturesAsync()) {
      if (alternateCulture === culture) {
        continue;
      }

      const alternateContext = await getCmsDetailPageContext(
        alternateCulture,
        slug,
      );

      const alternatePage = alternateContext.detailContext.pageResult.data;
      if (alternatePage) {
        redirect(
          appendSearchParams(buildCmsPagePath(slug), undefined, {
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
      localizeHref(buildCmsPagePath(page.slug), culture),
    );
  }

  return (
    <HelpPageDetail
      culture={culture}
      page={page}
      status={pageResult.status}
      message={pageResult.message}
      relatedPages={relatedPages}
    />
  );
}
