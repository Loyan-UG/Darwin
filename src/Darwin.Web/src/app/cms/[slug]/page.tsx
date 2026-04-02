import { notFound } from "next/navigation";
import { CmsPageDetail } from "@/components/cms/cms-page-detail";
import {
  getPublicPageBySlug,
  getPublishedPages,
} from "@/features/cms/api/public-cms";
import { getRequestCulture } from "@/lib/request-culture";
import { getSharedResource } from "@/localization";
import { buildSeoMetadata, deriveSeoDescription } from "@/lib/seo";

type CmsPageProps = {
  params: Promise<{
    slug: string;
  }>;
};

export async function generateMetadata({ params }: CmsPageProps) {
  const culture = await getRequestCulture();
  const shared = getSharedResource(culture);
  const { slug } = await params;
  const pageResult = await getPublicPageBySlug(slug);
  const page = pageResult.data;
  const path = `/cms/${encodeURIComponent(slug)}`;

  if (!page) {
    return buildSeoMetadata({
      culture,
      title:
        pageResult.status === "not-found"
          ? shared.cmsPageNotFoundTitle
          : shared.cmsPageUnavailableTitle,
      description: shared.cmsFallbackMetaDescription,
      path,
      noIndex: true,
    });
  }

  return buildSeoMetadata({
    culture,
    title: page.metaTitle ?? page.title,
    description:
      deriveSeoDescription(page.metaDescription, page.contentHtml) ??
      shared.cmsFallbackMetaDescription,
    path,
    noIndex: pageResult.status !== "ok",
    type: "article",
  });
}

export default async function CmsPage({ params }: CmsPageProps) {
  const culture = await getRequestCulture();
  const { slug } = await params;
  const [pageResult, relatedPagesSeed] = await Promise.all([
    getPublicPageBySlug(slug),
    getPublishedPages({
      page: 1,
      pageSize: 8,
    }),
  ]);
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
      relatedPages={relatedPagesSeed.data?.items ?? []}
      relatedStatus={relatedPagesSeed.status}
    />
  );
}
