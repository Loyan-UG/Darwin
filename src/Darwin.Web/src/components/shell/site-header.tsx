import Link from "next/link";
import { CultureSwitcher } from "@/components/shell/culture-switcher";
import { StatusBanner } from "@/components/feedback/status-banner";
import { getShellCopy } from "@/features/shell/copy";
import type { ShellLink } from "@/features/shell/types";
import { formatResource, getSharedResource } from "@/localization";

type SiteHeaderProps = {
  navigation: ShellLink[];
  utilityLinks: ShellLink[];
  activeThemeName: string;
  culture: string;
  supportedCultures: string[];
  menuSource: "cms" | "fallback";
  menuStatus:
    | "ok"
    | "empty-menu"
    | "not-found"
    | "network-error"
    | "http-error"
    | "invalid-payload";
  menuMessage?: string;
};

export function SiteHeader({
  navigation,
  utilityLinks,
  activeThemeName,
  culture,
  supportedCultures,
  menuSource,
  menuStatus,
  menuMessage,
}: SiteHeaderProps) {
  const copy = getShellCopy(culture);
  const shared = getSharedResource(culture);
  const localizedMenuSource = shared.menuSourceValues[menuSource];
  const fallbackMessage = menuMessage ??
    formatResource(shared.menuMessages.fallbackDefault, { menuStatus });

  return (
    <header className="sticky top-0 z-40 border-b border-[var(--color-border-soft)] bg-[rgba(248,244,231,0.9)] backdrop-blur-xl">
      <div className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-col gap-4 px-5 py-4 sm:px-6 lg:px-8">
        <div className="flex flex-wrap items-center justify-between gap-3 text-xs uppercase tracking-[0.24em] text-[var(--color-text-muted)]">
          <div className="flex items-center gap-3">
            <span className="inline-flex h-2.5 w-2.5 rounded-full bg-[var(--color-brand)]" />
            <span>{activeThemeName}</span>
          </div>
          <div className="flex items-center gap-3">
            <span>{copy.menuSourceLabel}: {localizedMenuSource}</span>
            <span className="hidden h-1 w-1 rounded-full bg-[var(--color-border-strong)] sm:inline-flex" />
            <span>{copy.shellBadge}</span>
          </div>
        </div>

        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex items-center gap-4">
            <Link href="/" className="inline-flex items-center gap-3">
              <span className="inline-flex h-12 w-12 items-center justify-center rounded-2xl bg-[var(--color-brand)] text-lg font-bold text-[var(--color-brand-contrast)]">
                D
              </span>
              <span>
                <span className="block font-[family-name:var(--font-display)] text-2xl leading-none text-[var(--color-text-primary)]">
                  Darwin
                </span>
                <span className="block text-sm text-[var(--color-text-secondary)]">
                  {copy.shellTagline}
                </span>
              </span>
            </Link>
          </div>

          <nav aria-label="Primary" className="flex flex-wrap items-center gap-2">
            {navigation.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                className="rounded-full border border-transparent px-4 py-2 text-sm font-semibold text-[var(--color-text-secondary)] transition hover:border-[var(--color-border-soft)] hover:bg-[var(--color-surface-panel)] hover:text-[var(--color-brand)]"
              >
                {link.label}
              </Link>
            ))}
          </nav>

          <div className="flex flex-wrap items-center gap-2">
            <CultureSwitcher
              currentCulture={culture}
              supportedCultures={supportedCultures}
            />
            {utilityLinks.map((link, index) => (
              <Link
                key={link.href}
                href={link.href}
                className={
                  index === 0
                    ? "rounded-full bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                    : "rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                }
              >
                {link.label}
              </Link>
            ))}
          </div>
        </div>

        {menuSource === "fallback" && (
          <StatusBanner
            tone="warning"
            title={copy.cmsFallbackTitle}
            message={fallbackMessage}
          />
        )}
      </div>
    </header>
  );
}
