import Link from "next/link";
import { localizeHref } from "@/lib/locale-routing";
import { getSharedResource } from "@/localization";
import type { WebPagePart } from "@/web-parts/types";

type PageComposerProps = {
  parts: WebPagePart[];
  culture: string;
};

export function PageComposer({ parts, culture }: PageComposerProps) {
  const shared = getSharedResource(culture);
  const heroPart = parts.find((part) => part.kind === "hero");
  const sectionLinks = parts
    .filter((part) => part.kind !== "hero")
    .map((part) => ({
      id: part.id,
      label: part.eyebrow.trim().length > 0 ? part.eyebrow : part.title,
      title: part.title,
    }));
  const quickNavTitle = heroPart?.eyebrow ?? sectionLinks[0]?.label ?? "";
  const quickNavDescription = heroPart?.title ?? sectionLinks[0]?.title ?? "";

  return (
    <div className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 flex-col gap-8 px-5 py-10 sm:px-6 lg:px-8">
      {sectionLinks.length > 0 ? (
        <section className="sticky top-4 z-10 rounded-[1rem] border border-[var(--color-border-soft)] bg-[color:color-mix(in_srgb,var(--color-surface-panel)_92%,white_8%)] px-5 py-5 shadow-[var(--shadow-panel)] backdrop-blur sm:px-6">
          <div className="flex flex-col gap-4">
            <div className="flex flex-wrap items-end justify-between gap-3">
              <div className="space-y-1">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-accent)]">
                  {quickNavTitle}
                </p>
                <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                  {quickNavDescription}
                </p>
              </div>
              {heroPart ? (
                <a
                  href={`#${heroPart.id}`}
                  className="inline-flex items-center rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {heroPart.title}
                </a>
              ) : null}
            </div>
            <div className="flex flex-wrap gap-2">
            {sectionLinks.map((section) => (
              <a
                key={section.id}
                href={`#${section.id}`}
                className="inline-flex items-center rounded-full border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                title={section.title}
              >
                {section.label}
              </a>
            ))}
          </div>
          </div>
        </section>
      ) : null}
      {parts.map((part) => {
        if (part.kind === "hero") {
          return (
            <section
              key={part.id}
              id={part.id}
              className="scroll-mt-28 overflow-hidden rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] shadow-[var(--shadow-panel)]"
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

                <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-6">
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
              id={part.id}
              className="scroll-mt-24 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10"
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
                    className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-5"
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
              id={part.id}
              className="scroll-mt-24 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10"
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
                      className="flex h-full flex-col rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-5"
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
                          {card.ctaLabel ?? shared.openLinkCta}
                        </Link>
                      </div>
                    </article>
                  ))}
                </div>
              ) : (
                <div className="mt-8 rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-6 py-10 text-center text-sm leading-7 text-[var(--color-text-secondary)]">
                  {part.emptyMessage}
                </div>
              )}
            </section>
          );
        }

        if (part.kind === "link-list") {
          return (
            <section
              key={part.id}
              id={part.id}
              className="scroll-mt-24 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10"
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

              {part.items.length > 0 ? (
                <div className="mt-8 grid gap-4 lg:grid-cols-3">
                  {part.items.map((item) => (
                    <article
                      key={item.id}
                      className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-5"
                    >
                      {item.meta ? (
                        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                          {item.meta}
                        </p>
                      ) : null}
                      <h3 className="mt-3 text-xl font-semibold text-[var(--color-text-primary)]">
                        {item.title}
                      </h3>
                      <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {item.description}
                      </p>
                      <div className="mt-5">
                        <Link
                          href={localizeHref(item.href, culture)}
                          className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                        >
                          {item.ctaLabel}
                        </Link>
                      </div>
                    </article>
                  ))}
                </div>
              ) : (
                <div className="mt-8 rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-6 py-10 text-center text-sm leading-7 text-[var(--color-text-secondary)]">
                  {part.emptyMessage}
                </div>
              )}
            </section>
          );
        }

        if (part.kind === "status-list") {
          return (
            <section
              key={part.id}
              id={part.id}
              className="scroll-mt-24 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10"
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

              {part.items.length > 0 ? (
                <div className="mt-8 grid gap-4 xl:grid-cols-3">
                  {part.items.map((item) => (
                    <article
                      key={item.id}
                      className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-5"
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                            {item.label}
                          </p>
                          <h3 className="mt-3 text-xl font-semibold text-[var(--color-text-primary)]">
                            {item.title}
                          </h3>
                        </div>
                        <span
                          className={
                            item.tone === "warning"
                              ? "inline-flex rounded-full bg-[rgba(217,111,50,0.12)] px-3 py-1 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]"
                              : "inline-flex rounded-full bg-[rgba(89,130,70,0.12)] px-3 py-1 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-brand)]"
                          }
                        >
                          {item.tone === "warning" ? shared.statusWarningLabel : shared.statusOkLabel}
                        </span>
                      </div>
                      <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {item.description}
                      </p>
                      {item.meta ? (
                        <p className="mt-4 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                          {item.meta}
                        </p>
                      ) : null}
                      <div className="mt-5">
                        <Link
                          href={localizeHref(item.href, culture)}
                          className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                        >
                          {item.ctaLabel}
                        </Link>
                      </div>
                    </article>
                  ))}
                </div>
              ) : (
                <div className="mt-8 rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-6 py-10 text-center text-sm leading-7 text-[var(--color-text-secondary)]">
                  {part.emptyMessage}
                </div>
              )}
            </section>
          );
        }

        if (part.kind === "stage-flow") {
          return (
            <section
              key={part.id}
              id={part.id}
              className="scroll-mt-24 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10"
            >
              <div className="max-w-3xl">
                <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
                  {part.eyebrow}
                </p>
                <h2 className="mt-4 font-[family-name:var(--font-display)] text-3xl leading-tight text-[var(--color-text-primary)] sm:text-4xl">
                  {part.title}
                </h2>
                <p className="mt-4 text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                  {part.description}
                </p>
              </div>

              {part.items.length > 0 ? (
                <div className="mt-8 grid gap-5 xl:grid-cols-3">
                  {part.items.map((item, index) => (
                    <article
                      key={item.id}
                      className="relative rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-5"
                    >
                      {index < part.items.length - 1 ? (
                        <span className="pointer-events-none absolute -right-3 top-10 hidden h-px w-6 bg-[var(--color-border-strong)] xl:block" />
                      ) : null}
                      <div className="inline-flex rounded-full bg-[rgba(89,130,70,0.12)] px-3 py-1 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-brand)]">
                        {item.step}
                      </div>
                      <h3 className="mt-4 text-xl font-semibold text-[var(--color-text-primary)]">
                        {item.title}
                      </h3>
                      <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {item.description}
                      </p>
                      {item.meta ? (
                        <p className="mt-4 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                          {item.meta}
                        </p>
                      ) : null}
                      <div className="mt-5">
                        <Link
                          href={localizeHref(item.href, culture)}
                          className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                        >
                          {item.ctaLabel}
                        </Link>
                      </div>
                    </article>
                  ))}
                </div>
              ) : (
                <div className="mt-8 rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-6 py-10 text-center text-sm leading-7 text-[var(--color-text-secondary)]">
                  {part.emptyMessage}
                </div>
              )}
            </section>
          );
        }

        if (part.kind === "pair-panel") {
          const panels = [part.leading, part.trailing];

          return (
            <section
              key={part.id}
              id={part.id}
              className="scroll-mt-24 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10"
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

              <div className="mt-8 grid gap-5 lg:grid-cols-2">
                {panels.map((panel, index) => (
                  <article
                    key={panel.id}
                    className={
                      index === 0
                        ? "rounded-[1rem] border border-[var(--color-border-soft)] bg-[linear-gradient(145deg,rgba(228,240,212,0.72),rgba(255,253,248,1))] p-5"
                        : "rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-5"
                    }
                  >
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {panel.eyebrow}
                    </p>
                    <h3 className="mt-4 text-2xl font-semibold text-[var(--color-text-primary)]">
                      {panel.title}
                    </h3>
                    <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {panel.description}
                    </p>
                    {panel.meta ? (
                      <p className="mt-4 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                        {panel.meta}
                      </p>
                    ) : null}
                    <div className="mt-5">
                      <Link
                        href={localizeHref(panel.href, culture)}
                        className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                      >
                        {panel.ctaLabel}
                      </Link>
                    </div>
                  </article>
                ))}
              </div>
            </section>
          );
        }

        if (part.kind === "agenda-columns") {
          return (
            <section
              key={part.id}
              id={part.id}
              className="scroll-mt-24 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10"
            >
              <div className="max-w-3xl">
                <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
                  {part.eyebrow}
                </p>
                <h2 className="mt-4 font-[family-name:var(--font-display)] text-3xl leading-tight text-[var(--color-text-primary)] sm:text-4xl">
                  {part.title}
                </h2>
                <p className="mt-4 text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                  {part.description}
                </p>
              </div>

              {part.columns.length > 0 ? (
                <div className="mt-8 grid gap-5 xl:grid-cols-3">
                  {part.columns.map((column) => (
                    <article
                      key={column.id}
                      className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-5"
                    >
                      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                        {column.label}
                      </p>
                      <h3 className="mt-4 text-2xl font-semibold text-[var(--color-text-primary)]">
                        {column.title}
                      </h3>
                      <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {column.description}
                      </p>
                      <div className="mt-5 flex flex-col gap-3">
                        {column.bullets.map((bullet) => (
                          <div
                            key={bullet}
                            className="rounded-2xl bg-[var(--color-surface-panel)] px-4 py-3 text-sm leading-7 text-[var(--color-text-secondary)]"
                          >
                            {bullet}
                          </div>
                        ))}
                      </div>
                      {column.meta ? (
                        <p className="mt-4 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                          {column.meta}
                        </p>
                      ) : null}
                      <div className="mt-5">
                        <Link
                          href={localizeHref(column.href, culture)}
                          className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                        >
                          {column.ctaLabel}
                        </Link>
                      </div>
                    </article>
                  ))}
                </div>
              ) : (
                <div className="mt-8 rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-6 py-10 text-center text-sm leading-7 text-[var(--color-text-secondary)]">
                  {part.emptyMessage}
                </div>
              )}
            </section>
          );
        }

        if (part.kind === "route-map") {
          return (
            <section
              key={part.id}
              id={part.id}
              className="scroll-mt-24 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10"
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

              {part.items.length > 0 ? (
                <div className="mt-8 grid gap-5 xl:grid-cols-3">
                  {part.items.map((item) => (
                    <article
                      key={item.id}
                      className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-5"
                    >
                      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                        {item.label}
                      </p>
                      <h3 className="mt-4 text-2xl font-semibold text-[var(--color-text-primary)]">
                        {item.title}
                      </h3>
                      <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {item.description}
                      </p>
                      {item.meta ? (
                        <p className="mt-4 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                          {item.meta}
                        </p>
                      ) : null}
                      <div className="mt-5 flex flex-wrap gap-3">
                        <Link
                          href={localizeHref(item.primaryHref, culture)}
                          className="inline-flex rounded-full bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                        >
                          {item.primaryCtaLabel}
                        </Link>
                        {item.secondaryHref && item.secondaryCtaLabel ? (
                          <Link
                            href={localizeHref(item.secondaryHref, culture)}
                            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                          >
                            {item.secondaryCtaLabel}
                          </Link>
                        ) : null}
                      </div>
                    </article>
                  ))}
                </div>
              ) : (
                <div className="mt-8 rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-6 py-10 text-center text-sm leading-7 text-[var(--color-text-secondary)]">
                  {part.emptyMessage}
                </div>
              )}
            </section>
          );
        }

          return (
            <section
              key={part.id}
              id={part.id}
              className="scroll-mt-24 rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-8 text-center shadow-[var(--shadow-panel)] sm:px-8 sm:py-10"
            >
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
              {part.eyebrow}
            </p>
            <h2 className="mt-4 font-[family-name:var(--font-display)] text-3xl leading-tight text-[var(--color-text-primary)] sm:text-4xl">
              {part.title}
            </h2>
            <p className="mx-auto mt-4 max-w-2xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
              {part.description}
            </p>
            <div className="mt-8 flex flex-wrap justify-center gap-3">
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
          </section>
        );
      })}
    </div>
  );
}
