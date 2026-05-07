import Link from "next/link";
import { CultureSwitcher } from "@/components/shell/culture-switcher";
import type { ShellLink } from "@/features/shell/types";
import { localizeHref } from "@/lib/locale-routing";

type SiteHeaderTemplateProps = {
  navigation: ShellLink[];
  utilityLinks: ShellLink[];
  culture: string;
  supportedCultures: string[];
  languageAlternates?: Record<string, string>;
  brandName: string;
  primaryNavigationLabel: string;
};

function getUtilityIcon(href: string) {
  if (href === "/cart") {
    return (
      <svg viewBox="0 0 24 24" aria-hidden="true" className="h-5 w-5">
        <path d="M3 5h2l1.2 7.2a2 2 0 0 0 2 1.7h7.8a2 2 0 0 0 2-1.5L20 8H7" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
        <circle cx="10" cy="19" r="1.5" fill="currentColor" />
        <circle cx="17" cy="19" r="1.5" fill="currentColor" />
      </svg>
    );
  }

  if (href === "/checkout") {
    return (
      <svg viewBox="0 0 24 24" aria-hidden="true" className="h-5 w-5">
        <path d="M7 4.5h10a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2v-11a2 2 0 0 1 2-2Z" fill="none" stroke="currentColor" strokeWidth="1.8" />
        <path d="M9 9h6M9 13h6M9 17h4" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      </svg>
    );
  }

  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className="h-5 w-5">
      <path d="M4 12.5 12 5l8 7.5" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M6.5 10.5v8h11v-8" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
    </svg>
  );
}

export function SiteHeaderTemplate({
  navigation,
  utilityLinks,
  culture,
  supportedCultures,
  languageAlternates,
  brandName,
  primaryNavigationLabel,
}: SiteHeaderTemplateProps) {
  return (
    <header className="sticky top-0 z-40 border-b border-[var(--color-border-soft)] bg-[rgba(252,255,247,0.9)] backdrop-blur-xl">
      <div className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-col gap-4 px-5 py-4 sm:px-6 lg:px-8">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex items-center gap-4">
            <Link
              href={localizeHref("/", culture)}
              className="inline-flex items-center gap-3"
            >
              <img
                src="/images/DarwinJustLogo.png"
                alt=""
                aria-hidden="true"
                className="h-12 w-12 object-contain"
              />
              <span className="block font-[family-name:var(--font-display)] text-2xl leading-none text-[var(--color-text-primary)]">
                {brandName}
              </span>
            </Link>
          </div>

          <nav aria-label={primaryNavigationLabel} className="flex flex-wrap items-center gap-2">
            {navigation.map((link) => (
              <Link
                key={link.href}
                href={localizeHref(link.href, culture)}
                className="rounded-full border border-transparent px-4 py-2 text-sm font-semibold text-[var(--color-text-secondary)] transition hover:border-[rgba(47,125,50,0.12)] hover:bg-white hover:text-[var(--color-brand)]"
              >
                {link.label}
              </Link>
            ))}
          </nav>

          <div className="flex flex-wrap items-center gap-2">
            <CultureSwitcher
              currentCulture={culture}
              supportedCultures={supportedCultures}
              languageAlternates={languageAlternates}
            />
            {utilityLinks.map((link) => (
              <Link
                key={link.href}
                href={localizeHref(link.href, culture)}
                aria-label={link.label}
                title={link.label}
                className="inline-flex h-10 w-10 items-center justify-center rounded-full border border-[var(--color-border-soft)] bg-white/86 text-[var(--color-text-primary)] transition hover:border-[rgba(47,125,50,0.18)] hover:bg-white hover:text-[var(--color-brand)]"
              >
                {getUtilityIcon(link.href)}
                <span className="sr-only">{link.label}</span>
              </Link>
            ))}
          </div>
        </div>
      </div>
    </header>
  );
}
