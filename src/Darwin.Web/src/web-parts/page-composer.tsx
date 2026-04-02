import Link from "next/link";
import { localizeHref } from "@/lib/locale-routing";
import type { WebPagePart } from "@/web-parts/types";

type PageComposerProps = {
  parts: WebPagePart[];
  culture: string;
};

export function PageComposer({ parts, culture }: PageComposerProps) {
  return (
    <div className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 flex-col gap-8 px-5 py-10 sm:px-6 lg:px-8">
      {parts.map((part) => {
        if (part.kind === "hero") {
          return (
            <section
              key={part.id}
              className="overflow-hidden rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] shadow-[var(--shadow-panel)]"
            >
              <div className="grid gap-6 px-6 py-8 sm:px-8 sm:py-10 lg:grid-cols-[1.15fr_0.85fr] lg:items-end">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.28em] text-[var(--color-brand)]">
                    {part.eyebrow}
                  </p>
                  <h1 className="mt-4 max-w-3xl font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl lg:text-6xl">
                    {part.title}
                  </h1>
                  <p className="mt-5 max-w-2xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                    {part.description}
                  </p>
                  <div className="mt-8 flex flex-wrap gap-3">
                    {part.actions.map((action) => (
                      <Link
                        key={action.href}
                        href={localizeHref(action.href, culture)}
                        className={
                          action.variant === "secondary"
                            ? "inline-flex items-center rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                            : "inline-flex items-center rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                        }
                      >
                        {action.label}
                      </Link>
                    ))}
                  </div>
                </div>

                <div className="rounded-[1.75rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-6">
                  <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-accent)]">
                    {part.panelTitle ?? part.eyebrow}
                  </p>
                  <div className="mt-5 space-y-4">
                    {part.highlights.map((note) => (
                      <div
                        key={note}
                        className="rounded-2xl bg-[var(--color-surface-panel)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]"
                      >
                        {note}
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </section>
          );
        }

        if (part.kind === "stat-grid") {
          return (
            <section
              key={part.id}
              className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10"
            >
              <div className="max-w-3xl">
                <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
                  {part.eyebrow}
                </p>
                <h2 className="mt-4 font-[family-name:var(--font-display)] text-3xl leading-tight text-[var(--color-text-primary)] sm:text-4xl">
                  {part.title}
                </h2>
                <p className="mt-4 text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                  {part.description}
                </p>
              </div>

              <div className="mt-8 grid gap-5 sm:grid-cols-2 xl:grid-cols-4">
                {part.metrics.map((metric) => (
                  <article
                    key={metric.id}
                    className="rounded-[1.75rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-5"
                  >
                    <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-text-muted)]">
                      {metric.label}
                    </p>
                    <p className="mt-3 text-3xl font-semibold text-[var(--color-text-primary)]">
                      {metric.value}
                    </p>
                    <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {metric.note}
                    </p>
                  </article>
                ))}
              </div>
            </section>
          );
        }

        if (part.kind === "card-grid") {
          return (
            <section
              key={part.id}
              className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10"
            >
              <div className="max-w-3xl">
                <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
                  {part.eyebrow}
                </p>
                <h2 className="mt-4 font-[family-name:var(--font-display)] text-3xl leading-tight text-[var(--color-text-primary)] sm:text-4xl">
                  {part.title}
                </h2>
                <p className="mt-4 text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                  {part.description}
                </p>
              </div>

              {part.cards.length > 0 ? (
                <div className="mt-8 grid gap-5 md:grid-cols-2 xl:grid-cols-3">
                  {part.cards.map((card) => (
                    <article
                      key={card.id}
                      className="flex h-full flex-col rounded-[1.75rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-5"
                    >
                      {card.eyebrow ? (
                        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                          {card.eyebrow}
                        </p>
                      ) : null}
                      <h3 className="mt-3 text-xl font-semibold text-[var(--color-text-primary)]">
                        {card.title}
                      </h3>
                      <p className="mt-3 flex-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {card.description}
                      </p>
                      {card.meta ? (
                        <p className="mt-4 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                          {card.meta}
                        </p>
                      ) : null}
                      <div className="mt-5">
                        <Link
                          href={localizeHref(card.href, culture)}
                          className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                        >
                          {card.ctaLabel ?? "Open"}
                        </Link>
                      </div>
                    </article>
                  ))}
                </div>
              ) : (
                <div className="mt-8 rounded-[1.75rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-6 py-10 text-center text-sm leading-7 text-[var(--color-text-secondary)]">
                  {part.emptyMessage}
                </div>
              )}
            </section>
          );
        }

        return (
          <section
            key={part.id}
            className="overflow-hidden rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] shadow-[var(--shadow-panel)]"
          >
            <div className="grid gap-6 px-6 py-8 sm:px-8 sm:py-10 lg:grid-cols-[1.1fr_0.9fr] lg:items-end">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.28em] text-[var(--color-brand)]">
                  {part.eyebrow}
                </p>
                <h1 className="mt-4 max-w-3xl font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl lg:text-6xl">
                  {part.title}
                </h1>
                <p className="mt-5 max-w-2xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                  {part.description}
                </p>
                <div className="mt-8 flex flex-wrap gap-3">
                  {part.actions.map((action) => (
                    <Link
                      key={action.href}
                      href={localizeHref(action.href, culture)}
                      className={
                        action.variant === "secondary"
                          ? "inline-flex items-center rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                          : "inline-flex items-center rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                      }
                    >
                      {action.label}
                    </Link>
                  ))}
                </div>
              </div>

              <div className="rounded-[1.75rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-6">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-accent)]">
                  Page-part model
                </p>
                <div className="mt-5 space-y-4">
                  {[
                    "Theme tokens and shell chrome stay outside feature slices.",
                    "Each page can be composed from explicit web parts instead of one-off layouts.",
                    "Home can remain light while later merchandising parts are added incrementally.",
                  ].map((note) => (
                    <div
                      key={note}
                      className="rounded-2xl bg-[var(--color-surface-panel)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]"
                    >
                      {note}
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </section>
        );
      })}
    </div>
  );
}
