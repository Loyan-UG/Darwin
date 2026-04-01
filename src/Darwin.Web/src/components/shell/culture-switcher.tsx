"use client";

import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import { getCultureDisplayName } from "@/lib/culture";

type CultureSwitcherProps = {
  currentCulture: string;
  supportedCultures: string[];
};

function buildCultureHref(
  pathname: string,
  searchParams: URLSearchParams,
  culture: string,
) {
  const params = new URLSearchParams(searchParams.toString());
  params.set("culture", culture);
  const query = params.toString();
  return query ? `${pathname}?${query}` : pathname;
}

export function CultureSwitcher({
  currentCulture,
  supportedCultures,
}: CultureSwitcherProps) {
  const pathname = usePathname();
  const searchParams = useSearchParams();

  return (
    <div className="flex flex-wrap items-center gap-2">
      {supportedCultures.map((culture) => {
        const isActive = culture === currentCulture;

        return (
          <Link
            key={culture}
            href={buildCultureHref(pathname, searchParams, culture)}
            className={
              isActive
                ? "rounded-full bg-[var(--color-brand)] px-3 py-2 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-brand-contrast)]"
                : "rounded-full border border-[var(--color-border-soft)] px-3 py-2 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
            }
          >
            {getCultureDisplayName(culture)}
          </Link>
        );
      })}
    </div>
  );
}
