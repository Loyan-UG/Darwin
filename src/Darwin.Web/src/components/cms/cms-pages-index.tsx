import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicPageSummary } from "@/features/cms/types";

type CmsPagesIndexProps = {
  pages: PublicPageSummary[];
  totalPages: number;
  currentPage: number;
  status: string;
};

function buildCmsHref(page = 1) {
  return page > 1 ? `/cms?page=${page}` : "/cms";
}

export function CmsPagesIndex({
  pages,
  totalPages,
  currentPage,
  status,
}: CmsPagesIndexProps) {
  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            Public CMS
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            Published content pages from `Darwin.WebApi`
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            This route consumes the public CMS page list directly. It is meant to validate that the storefront sees the same published content truth that WebAdmin manages.
          </p>
        </div>

        {status !== "ok" && (
          <StatusBanner
            tone="warning"
            title="CMS listing is running in degraded mode."
            message={`The public CMS pages endpoint returned status "${status}". This failure stays visible in the UI instead of collapsing into a silent placeholder.`}
          />
        )}

        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {pages.map((page) => (
            <article
              key={page.id}
              className="flex h-full flex-col rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]"
            >
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                CMS page
              </p>
              <h2 className="mt-4 text-2xl font-semibold text-[var(--color-text-primary)]">
                <Link href={`/cms/${page.slug}`} className="transition hover:text-[var(--color-brand)]">
                  {page.title}
                </Link>
              </h2>
              <p className="mt-4 flex-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                {page.metaDescription ?? "Published CMS page available through the public storefront content contract."}
              </p>
              <div className="mt-6">
                <Link
                  href={`/cms/${page.slug}`}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  Open page
                </Link>
              </div>
            </article>
          ))}
        </div>

        {pages.length === 0 && (
          <div className="rounded-[2rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center shadow-[var(--shadow-panel)]">
            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
              No published CMS pages were returned for this page of the public content surface.
            </p>
          </div>
        )}

        {totalPages > 1 && (
          <div className="flex flex-wrap items-center gap-3">
            <Link
              aria-disabled={currentPage <= 1}
              href={buildCmsHref(Math.max(1, currentPage - 1))}
              className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
            >
              Previous
            </Link>
            <p className="text-sm text-[var(--color-text-secondary)]">
              Page {currentPage} of {totalPages}
            </p>
            <Link
              aria-disabled={currentPage >= totalPages}
              href={buildCmsHref(Math.min(totalPages, currentPage + 1))}
              className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
            >
              Next
            </Link>
          </div>
        )}
      </div>
    </section>
  );
}
