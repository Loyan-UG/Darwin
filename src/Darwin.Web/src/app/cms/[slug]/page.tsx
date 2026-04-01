import { notFound } from "next/navigation";
import { CmsPageDetail } from "@/components/cms/cms-page-detail";
import {
  getPublicPageBySlug,
  getPublishedPages,
} from "@/features/cms/api/public-cms";

type CmsPageProps = {
  params: Promise<{
    slug: string;
  }>;
};

export async function generateMetadata({ params }: CmsPageProps) {
  const { slug } = await params;
  const pageResult = await getPublicPageBySlug(slug);
  const page = pageResult.data;

  if (!page) {
    return {
      title: pageResult.status === "not-found" ? "Page not found" : "CMS page unavailable",
    };
  }

  return {
    title: page.metaTitle ?? page.title,
    description:
      page.metaDescription ?? "Published CMS content delivered from Darwin.WebApi.",
  };
}

export default async function CmsPage({ params }: CmsPageProps) {
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
      page={page}
      status={pageResult.status}
      message={pageResult.message}
      relatedPages={relatedPagesSeed.data?.items ?? []}
      relatedStatus={relatedPagesSeed.status}
    />
  );
}
