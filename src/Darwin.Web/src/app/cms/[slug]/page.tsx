import { notFound } from "next/navigation";
import { getPublicPageBySlug } from "@/features/cms/api/public-cms";

type CmsPageProps = {
  params: Promise<{
    slug: string;
  }>;
};

export default async function CmsPage({ params }: CmsPageProps) {
  const { slug } = await params;
  const pageResult = await getPublicPageBySlug(slug);
  const page = pageResult.data;

  if (!page) {
    notFound();
  }

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <article className="w-full rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8 lg:px-12">
        <div className="max-w-3xl">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            CMS page
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {page.title}
          </h1>
        </div>
        <div
          className="cms-content mt-8 max-w-none"
          dangerouslySetInnerHTML={{ __html: page.contentHtml }}
        />
      </article>
    </section>
  );
}
