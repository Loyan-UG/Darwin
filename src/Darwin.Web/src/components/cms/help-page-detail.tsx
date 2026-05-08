import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { summarizeCmsContent } from "@/features/cms/content-summary";
import type { PublicPageDetail, PublicPageSummary } from "@/features/cms/types";
import { buildCmsPagePath } from "@/lib/entity-paths";
import { resolveRelativeHtmlMediaUrls, sanitizeHtmlFragment } from "@/lib/html-fragment";
import { localizeHref } from "@/lib/locale-routing";
import { toWebApiUrl } from "@/lib/webapi-url";
import { resolveLocalizedQueryMessage } from "@/localization";

type HelpPageDetailProps = {
  culture: string;
  page: PublicPageDetail | null;
  status: string;
  message?: string;
  relatedPages: PublicPageSummary[];
};

function getCopy(culture: string) {
  const english = culture.toLowerCase().startsWith("en");

  return {
    home: english ? "Home" : "Start",
    help: english ? "Help and information" : "Hilfe und Informationen",
    unavailableTitle: english ? "This page is temporarily unavailable." : "Diese Seite ist voruebergehend nicht verfuegbar.",
    unavailableMessage: english ? "Please return to help and try again later." : "Bitte kehren Sie zur Hilfe zurueck und versuchen Sie es spaeter erneut.",
    backToHelp: english ? "Back to help" : "Zurueck zur Hilfe",
    related: english ? "Related pages" : "Weitere Seiten",
    read: english ? "Read" : "Lesen",
  };
}

export function HelpPageDetail({
  culture,
  page,
  status,
  message,
  relatedPages,
}: HelpPageDetailProps) {
  const copy = getCopy(culture);
  const resolvedMessage = resolveLocalizedQueryMessage(message, copy);
  const contentSummary = page ? summarizeCmsContent(page.contentHtml) : null;
  const sanitizedContentHtml = page
    ? resolveRelativeHtmlMediaUrls(
        contentSummary?.html ?? sanitizeHtmlFragment(page.contentHtml),
        toWebApiUrl,
      )
    : "";

  if (!page) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
        <div className="w-full rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title={copy.unavailableTitle}
            message={resolvedMessage ?? copy.unavailableMessage}
          />
          <Link
            href={localizeHref("/help", culture)}
            className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)]"
          >
            {copy.backToHelp}
          </Link>
        </div>
      </section>
    );
  }

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <article className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8 lg:px-12">
          <nav
            aria-label="Breadcrumb"
            className="flex flex-wrap items-center gap-2 text-sm text-[var(--color-text-secondary)]"
          >
            <Link href={localizeHref("/", culture)} className="transition hover:text-[var(--color-brand)]">
              {copy.home}
            </Link>
            <span>/</span>
            <Link href={localizeHref("/help", culture)} className="transition hover:text-[var(--color-brand)]">
              {copy.help}
            </Link>
            <span>/</span>
            <span className="font-medium text-[var(--color-text-primary)]">
              {page.title}
            </span>
          </nav>

          <div className="mt-8 max-w-3xl">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
              {copy.help}
            </p>
            <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
              {page.title}
            </h1>
          </div>

          {status !== "ok" ? (
            <div className="mt-8">
              <StatusBanner
                tone="warning"
                title={copy.unavailableTitle}
                message={resolvedMessage ?? copy.unavailableMessage}
              />
            </div>
          ) : null}

          <div
            className="content-body mt-8 max-w-none"
            dangerouslySetInnerHTML={{ __html: sanitizedContentHtml }}
          />
        </article>

        {relatedPages.length > 0 ? (
          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-white/92 px-6 py-6 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.related}
            </p>
            <div className="mt-5 grid gap-4 md:grid-cols-3">
              {relatedPages.slice(0, 3).map((related) => (
                <Link
                  key={related.id}
                  href={localizeHref(buildCmsPagePath(related.slug), culture)}
                  className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-4 transition hover:bg-[var(--color-surface-panel)]"
                >
                  <p className="font-semibold text-[var(--color-text-primary)]">
                    {related.title}
                  </p>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {related.metaDescription ?? copy.help}
                  </p>
                  <span className="mt-4 inline-flex text-sm font-semibold text-[var(--color-brand)]">
                    {copy.read}
                  </span>
                </Link>
              ))}
            </div>
          </section>
        ) : null}
      </div>
    </section>
  );
}
