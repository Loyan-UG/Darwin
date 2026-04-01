import { getShellCopy } from "@/features/shell/copy";
import type { ShellLinkGroup } from "@/features/shell/types";

type SiteFooterProps = {
  groups: ShellLinkGroup[];
  culture: string;
};

export function SiteFooter({ groups, culture }: SiteFooterProps) {
  const copy = getShellCopy(culture);

  return (
    <footer className="border-t border-[var(--color-border-soft)] bg-[rgba(255,253,248,0.9)]">
      <div className="mx-auto grid w-full max-w-[var(--content-max-width)] gap-10 px-5 py-10 sm:px-6 lg:grid-cols-[1.1fr_repeat(3,minmax(0,1fr))] lg:px-8">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-[var(--color-brand)]">
            {copy.footerEyebrow}
          </p>
          <h2 className="mt-4 font-[family-name:var(--font-display)] text-2xl text-[var(--color-text-primary)]">
            {copy.footerTitle}
          </h2>
          <p className="mt-4 max-w-sm text-sm leading-7 text-[var(--color-text-secondary)]">
            {copy.footerDescription}
          </p>
        </div>

        {groups.map((group) => (
          <div key={group.title}>
            <p className="text-sm font-semibold text-[var(--color-text-primary)]">
              {group.title}
            </p>
            <ul className="mt-4 space-y-3 text-sm text-[var(--color-text-secondary)]">
              {group.links.map((link) => (
                <li key={link.href}>
                  <a className="transition hover:text-[var(--color-brand)]" href={link.href}>
                    {link.label}
                  </a>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>
    </footer>
  );
}
