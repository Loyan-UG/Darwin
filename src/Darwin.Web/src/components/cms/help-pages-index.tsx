import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicPageSummary } from "@/features/cms/types";
import { buildCmsPagePath } from "@/lib/entity-paths";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import { formatResource } from "@/localization";

type HelpPagesIndexProps = {
  culture: string;
  pages: PublicPageSummary[];
  totalPages: number;
  currentPage: number;
  status: string;
  searchQuery?: string;
};

function buildHelpHref(page = 1, searchQuery?: string) {
  return buildAppQueryPath("/help", {
    page: page > 1 ? page : undefined,
    search: searchQuery,
  });
}

function getCopy(culture: string) {
  const english = culture.toLowerCase().startsWith("en");

  return {
    home: english ? "Home" : "Start",
    help: english ? "Help and information" : "Hilfe und Informationen",
    title: english ? "Help and information" : "Hilfe und Informationen",
    description: english
      ? "Find customer guides, policies, contact information, and answers for shopping with Darwin."
      : "Finden Sie Kundenratgeber, Richtlinien, Kontaktinformationen und Antworten zum Einkauf bei Darwin.",
    search: english ? "Search pages" : "Seiten suchen",
    placeholder: english ? "Search help topics" : "Hilfethemen suchen",
    sort: english ? "Sort" : "Sortierung",
    featured: english ? "Featured" : "Empfohlen",
    titleAsc: english ? "Title A-Z" : "Titel A-Z",
    apply: english ? "Search" : "Suchen",
    clear: english ? "Clear" : "Zuruecksetzen",
    unavailableTitle: english ? "Information pages are temporarily unavailable." : "Informationsseiten sind voruebergehend nicht verfuegbar.",
    unavailableMessage: english ? "Please try again later." : "Bitte versuchen Sie es spaeter erneut.",
    open: english ? "Read" : "Lesen",
    empty: english ? "No information pages found." : "Keine Informationsseiten gefunden.",
    emptyHint: english ? "Try another search term or clear the search." : "Versuchen Sie einen anderen Suchbegriff oder setzen Sie die Suche zurueck.",
    pageLabel: english ? "Page {currentPage} of {totalPages}" : "Seite {currentPage} von {totalPages}",
    previous: english ? "Previous" : "Zurueck",
    next: english ? "Next" : "Weiter",
  };
}

export function HelpPagesIndex({
  culture,
  pages,
  totalPages,
  currentPage,
  status,
  searchQuery,
}: HelpPagesIndexProps) {
  const copy = getCopy(culture);

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <nav
          aria-label="Breadcrumb"
          className="flex flex-wrap items-center gap-2 text-sm text-[var(--color-text-secondary)]"
        >
          <Link href={localizeHref("/", culture)} className="transition hover:text-[var(--color-brand)]">
            {copy.home}
          </Link>
          <span>/</span>
          <span className="font-medium text-[var(--color-text-primary)]">
            {copy.help}
          </span>
        </nav>

        <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.help}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.title}
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {copy.description}
          </p>
        </div>

        {status !== "ok" ? (
          <StatusBanner
            tone="warning"
            title={copy.unavailableTitle}
            message={copy.unavailableMessage}
          />
        ) : null}

        <form
          action={localizeHref("/help", culture)}
          className="rounded-[1rem] border border-[var(--color-border-soft)] bg-white/92 p-5 shadow-[0_22px_60px_rgba(38,76,34,0.08)]"
        >
          <div className="grid gap-4 md:grid-cols-[minmax(0,1fr)_auto_auto] md:items-end">
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.search}
              <input
                type="search"
                name="search"
                defaultValue={searchQuery ?? ""}
                placeholder={copy.placeholder}
                maxLength={80}
                autoComplete="off"
                className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
              />
            </label>
            <button
              type="submit"
              className="inline-flex justify-center rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              {copy.apply}
            </button>
            <Link
              href={localizeHref("/help", culture)}
              className="inline-flex justify-center rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.clear}
            </Link>
          </div>
        </form>

        {pages.length === 0 ? (
          <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center shadow-[var(--shadow-panel)]">
            <h2 className="font-[family-name:var(--font-display)] text-3xl text-[var(--color-text-primary)]">
              {copy.empty}
            </h2>
            <p className="mt-4 text-base leading-8 text-[var(--color-text-secondary)]">
              {copy.emptyHint}
            </p>
          </div>
        ) : (
          <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
            {pages.map((page) => (
              <article
                key={page.id}
                className="flex h-full flex-col rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]"
              >
                <h2 className="text-2xl font-semibold text-[var(--color-text-primary)]">
                  <Link
                    href={localizeHref(buildCmsPagePath(page.slug), culture)}
                    className="transition hover:text-[var(--color-brand)]"
                  >
                    {page.title}
                  </Link>
                </h2>
                <p className="mt-4 flex-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {page.metaDescription ?? copy.description}
                </p>
                <div className="mt-6">
                  <Link
                    href={localizeHref(buildCmsPagePath(page.slug), culture)}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.open}
                  </Link>
                </div>
              </article>
            ))}
          </div>
        )}

        {totalPages > 1 ? (
          <div className="flex flex-wrap items-center justify-center gap-3 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <Link
              aria-disabled={currentPage <= 1}
              href={localizeHref(
                buildHelpHref(Math.max(1, currentPage - 1), searchQuery),
                culture,
              )}
              className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
            >
              {copy.previous}
            </Link>
            <p className="text-sm text-[var(--color-text-secondary)]">
              {formatResource(copy.pageLabel, { currentPage, totalPages })}
            </p>
            <Link
              aria-disabled={currentPage >= totalPages}
              href={localizeHref(
                buildHelpHref(Math.min(totalPages, currentPage + 1), searchQuery),
                culture,
              )}
              className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
            >
              {copy.next}
            </Link>
          </div>
        ) : null}
      </div>
    </section>
  );
}
