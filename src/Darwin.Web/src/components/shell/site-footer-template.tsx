import type { ShellLinkGroup } from "@/features/shell/types";
import { localizeHref } from "@/lib/locale-routing";

type SiteFooterTemplateProps = {
  groups: ShellLinkGroup[];
  culture: string;
  columnCount: number;
  eyebrow: string;
  title: string;
  description: string;
};

export function SiteFooterTemplate({
  groups,
  culture,
  columnCount,
  eyebrow,
  title,
  description,
}: SiteFooterTemplateProps) {
  const resolvedColumnCount = Math.min(Math.max(columnCount, 2), 6);

  return (
    <footer className="border-t border-[var(--color-border-soft)] bg-[linear-gradient(180deg,rgba(255,255,255,0.94),rgba(241,248,230,0.96))]">
      <div className="mx-auto grid w-full max-w-[var(--content-max-width)] gap-10 px-5 py-10 sm:px-6 lg:grid-cols-[minmax(260px,0.8fr)_minmax(0,1.2fr)] lg:px-8">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-[var(--color-brand)]">
            {eyebrow}
          </p>
          <h2 className="mt-4 font-[family-name:var(--font-display)] text-2xl text-[var(--color-text-primary)]">
            {title}
          </h2>
          <p className="mt-4 max-w-sm text-sm leading-7 text-[var(--color-text-secondary)]">
            {description}
          </p>
        </div>

        <div className="grid gap-8">
          {groups.map((group) => (
            <div key={group.title}>
              <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                {group.title}
              </p>
              <ul
                className="mt-4 grid gap-x-8 gap-y-3 text-sm text-[var(--color-text-secondary)]"
                style={{ gridTemplateColumns: `repeat(${resolvedColumnCount}, minmax(0, 1fr))` }}
              >
                {group.links.map((link) => (
                  <li key={link.href}>
                    <a
                      className="transition hover:text-[var(--color-brand)]"
                      href={localizeHref(link.href, culture)}
                    >
                      {link.label}
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </div>
    </footer>
  );
}
