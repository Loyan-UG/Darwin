import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicPageSummary } from "@/features/cms/types";
import { localizeHref } from "@/lib/locale-routing";
import { formatResource, getSharedResource } from "@/localization";

type CmsPagesIndexProps = {
  culture: string;
  pages: PublicPageSummary[];
  totalPages: number;
  currentPage: number;
  status: string;
};

function buildCmsHref(page = 1) {
  return page > 1 ? `/cms?page=${page}` : "/cms";
}

export function CmsPagesIndex({
  culture,
  pages,
  totalPages,
  currentPage,
  status,
}: CmsPagesIndexProps) {
  const copy = getSharedResource(culture);

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.cmsIndexEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.cmsIndexTitle}
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {copy.cmsIndexDescription}
          </p>
        </div>

        {status !== "ok" && (
          <StatusBanner
            tone="warning"
            title={copy.cmsIndexDegradedTitle}
            message={formatResource(copy.cmsIndexDegradedMessage, { status })}
          />
        )}

        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {pages.map((page) => (
            <article
              key={page.id}
              className="flex h-full flex-col rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]"
            >
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.cmsPageEyebrow}
              </p>
              <h2 className="mt-4 text-2xl font-semibold text-[var(--color-text-primary)]">
                <Link
                  href={localizeHref(`/cms/${page.slug}`, culture)}
                  className="transition hover:text-[var(--color-brand)]"
                >
                  {page.title}
                </Link>
              </h2>
              <p className="mt-4 flex-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                {page.metaDescription ?? copy.cmsCardDescriptionFallback}
              </p>
              <div className="mt-6">
                <Link
                  href={localizeHref(`/cms/${page.slug}`, culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.cmsOpenPageCta}
                </Link>
              </div>
            </article>
          ))}
        </div>

        {pages.length === 0 && (
          <div className="rounded-[2rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center shadow-[var(--shadow-panel)]">
            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.cmsNoPagesMessage}
            </p>
          </div>
        )}

        {totalPages > 1 && (
          <div className="flex flex-wrap items-center gap-3">
            <Link
              aria-disabled={currentPage <= 1}
              href={localizeHref(buildCmsHref(Math.max(1, currentPage - 1)), culture)}
              className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
            >
              {copy.previous}
            </Link>
            <p className="text-sm text-[var(--color-text-secondary)]">
              {formatResource(copy.pageLabel, { currentPage, totalPages })}
            </p>
            <Link
              aria-disabled={currentPage >= totalPages}
              href={localizeHref(buildCmsHref(Math.min(totalPages, currentPage + 1)), culture)}
              className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
            >
              {copy.next}
            </Link>
          </div>
        )}
      </div>
    </section>
  );
}
