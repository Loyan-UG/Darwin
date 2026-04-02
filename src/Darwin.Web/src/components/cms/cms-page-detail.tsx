import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicPageDetail, PublicPageSummary } from "@/features/cms/types";
import { localizeHref } from "@/lib/locale-routing";
import {
  formatResource,
  getSharedResource,
  resolveLocalizedQueryMessage,
} from "@/localization";

type CmsPageDetailProps = {
  culture: string;
  page: PublicPageDetail | null;
  status: string;
  message?: string;
  relatedPages: PublicPageSummary[];
  relatedStatus: string;
};

export function CmsPageDetail({
  culture,
  page,
  status,
  message,
  relatedPages,
  relatedStatus,
}: CmsPageDetailProps) {
  const copy = getSharedResource(culture);
  const resolvedMessage = resolveLocalizedQueryMessage(message, copy);

  if (!page) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
        <div className="w-full rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title={copy.cmsPageUnavailableTitle}
            message={resolvedMessage ?? formatResource(copy.cmsDetailWarningsMessage, { status })}
          />
          <div className="mt-8">
            <Link
              href={localizeHref("/cms", culture)}
              className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              {copy.cmsOpenIndexCta}
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
              {copy.cmsPageEyebrow}
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
                title={copy.cmsDetailWarningsTitle}
                message={resolvedMessage ?? formatResource(copy.cmsDetailWarningsMessage, { status })}
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
              {copy.cmsContentNavigationTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.cmsContentNavigationDescription}
            </p>
            <div className="mt-5 flex flex-col gap-2">
              <Link
                href={localizeHref("/cms", culture)}
                className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.cmsAllPublishedPagesCta}
              </Link>
              {relatedPages.map((relatedPage) => (
                <Link
                  key={relatedPage.id}
                  href={localizeHref(`/cms/${relatedPage.slug}`, culture)}
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
              title={copy.cmsRelatedPagesDegradedTitle}
              message={formatResource(copy.cmsRelatedPagesDegradedMessage, {
                status: relatedStatus,
              })}
            />
          )}
        </aside>
      </div>
    </section>
  );
}
