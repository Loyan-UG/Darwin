import Link from "next/link";
import type { WebPagePart } from "@/web-parts/types";

type PageComposerProps = {
  parts: WebPagePart[];
};

export function PageComposer({ parts }: PageComposerProps) {
  return (
    <div className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 flex-col gap-8 px-5 py-10 sm:px-6 lg:px-8">
      {parts.map((part) => {
        if (part.kind !== "blank-state") {
          return null;
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
                      href={action.href}
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
