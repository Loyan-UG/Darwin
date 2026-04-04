"use client";

import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import { getCultureDisplayName, getCultureShortCode } from "@/lib/culture";
import { buildLocalizedPath, isPublicLocalizedPath, stripCulturePrefix } from "@/lib/locale-routing";
import { cloneSearchParams } from "@/lib/query-params";

type CultureSwitcherProps = {
  currentCulture: string;
  supportedCultures: string[];
};

function buildCultureHref(
  pathname: string,
  searchParams: URLSearchParams,
  culture: string,
) {
  const params = cloneSearchParams(searchParams);
  const strippedPath = stripCulturePrefix(pathname).pathname;

  if (isPublicLocalizedPath(strippedPath)) {
    params.delete("culture");
    const query = params.toString();
    const localizedPath = buildLocalizedPath(strippedPath, culture);
    return query ? `${localizedPath}?${query}` : localizedPath;
  }

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
            aria-label={getCultureDisplayName(culture)}
            title={getCultureDisplayName(culture)}
            className={
              isActive
                ? "inline-flex h-10 min-w-10 items-center justify-center rounded-full bg-[var(--color-brand)] px-3 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-brand-contrast)]"
                : "inline-flex h-10 min-w-10 items-center justify-center rounded-full border border-[var(--color-border-soft)] px-3 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
            }
          >
            {getCultureShortCode(culture)}
          </Link>
        );
      })}
    </div>
  );
}
