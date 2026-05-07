import Link from "next/link";
import { localizeHref } from "@/lib/locale-routing";

export type PublicContinuationItem = {
  id: string;
  label: string;
  title: string;
  description: string;
  href: string;
  ctaLabel: string;
};

type PublicContinuationRailProps = {
  culture: string;
  eyebrow: string;
  title: string;
  description: string;
  items: PublicContinuationItem[];
};

export function PublicContinuationRail({
  culture,
  eyebrow,
  title,
  description,
  items,
}: PublicContinuationRailProps) {
  return (
    <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
        {eyebrow}
      </p>
      <h2 className="mt-3 text-xl font-semibold text-[var(--color-text-primary)]">
        {title}
      </h2>
      <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
        {description}
      </p>
      <div className="mt-5 grid gap-3">
        {items.map((item) => (
          <article
            key={item.id}
            className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4"
          >
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {item.label}
            </p>
            <h3 className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
              {item.title}
            </h3>
            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
              {item.description}
            </p>
            <div className="mt-4">
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
    </div>
  );
}
