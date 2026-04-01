import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicPageDetail, PublicPageSummary } from "@/features/cms/types";

type CmsPageDetailProps = {
  page: PublicPageDetail | null;
  status: string;
  message?: string;
  relatedPages: PublicPageSummary[];
  relatedStatus: string;
};

export function CmsPageDetail({
  page,
  status,
  message,
  relatedPages,
  relatedStatus,
}: CmsPageDetailProps) {
  if (!page) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
        <div className="w-full rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title="CMS page is unavailable."
            message={message ?? `The public CMS page endpoint returned status "${status}".`}
          />
          <div className="mt-8">
            <Link
              href="/cms"
              className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              Open CMS index
            </Link>
          </div>
        </div>
      </section>
    );
  }

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="grid w-full gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <article className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8 lg:px-12">
          <div className="max-w-3xl">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
              CMS page
            </p>
            <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
              {page.title}
            </h1>
            {page.metaDescription ? (
              <p className="mt-5 text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                {page.metaDescription}
              </p>
            ) : null}
          </div>

          {status !== "ok" && (
            <div className="mt-8">
              <StatusBanner
                tone="warning"
                title="CMS detail loaded with warnings."
                message={message ?? `The page fetch reported status "${status}". The storefront is rendering the content it could resolve.`}
              />
            </div>
          )}

          <div
            className="cms-content mt-8 max-w-none"
            dangerouslySetInnerHTML={{ __html: page.contentHtml }}
          />
        </article>

        <aside className="flex flex-col gap-5">
          <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              Content navigation
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              Published CMS pages are surfaced here through the same public contract instead of theme-only hard-coded sidebars.
            </p>
            <div className="mt-5 flex flex-col gap-2">
              <Link
                href="/cms"
                className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                All published pages
              </Link>
              {relatedPages.map((relatedPage) => (
                <Link
                  key={relatedPage.id}
                  href={`/cms/${relatedPage.slug}`}
                  className={
                    relatedPage.slug === page.slug
                      ? "rounded-2xl bg-[var(--color-brand)] px-4 py-3 text-sm font-semibold text-[var(--color-brand-contrast)]"
                      : "rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  }
                >
                  {relatedPage.title}
                </Link>
              ))}
            </div>
          </div>

          {relatedStatus !== "ok" && (
            <StatusBanner
              tone="warning"
              title="Related-page listing is degraded."
              message={`The public CMS listing endpoint returned status "${relatedStatus}" while building this sidebar.`}
            />
          )}
        </aside>
      </div>
    </section>
  );
}
