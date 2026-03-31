import Link from "next/link";

type PlaceholderPageProps = {
  eyebrow: string;
  title: string;
  description: string;
  bullets: string[];
  primaryAction: {
    label: string;
    href: string;
  };
};

export function PlaceholderPage({
  eyebrow,
  title,
  description,
  bullets,
  primaryAction,
}: PlaceholderPageProps) {
  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="grid w-full gap-6 lg:grid-cols-[1.2fr_0.8fr]">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {eyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {title}
          </h1>
          <p className="mt-5 max-w-2xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {description}
          </p>
          <div className="mt-8">
            <Link
              href={primaryAction.href}
              className="inline-flex items-center rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              {primaryAction.label}
            </Link>
          </div>
        </div>

        <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
            Current shape
          </p>
          <ul className="mt-5 space-y-4 text-sm leading-7 text-[var(--color-text-secondary)] sm:text-base">
            {bullets.map((bullet) => (
              <li
                key={bullet}
                className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3"
              >
                {bullet}
              </li>
            ))}
          </ul>
        </aside>
      </div>
    </section>
  );
}
