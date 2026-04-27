"use client";

import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { getCultureDisplayName, getCultureShortCode } from "@/lib/culture";
import {
  INFERRED_CULTURE_SEARCH_PARAM,
  isPublicLocalizedPath,
  stripCulturePrefix,
} from "@/lib/locale-routing";
import { cloneSearchParams } from "@/lib/query-params";

type CultureSwitcherProps = {
  currentCulture: string;
  supportedCultures: string[];
  languageAlternates?: Record<string, string>;
};

function toLanguageAlternateMap(values?: Record<string, string>) {
  const alternates = new Map<string, string>();

  for (const [culture, path] of Object.entries(values ?? {})) {
    if (path) {
      alternates.set(culture, path);
    }
  }

  return alternates;
}

function readDocumentLanguageAlternates(supportedCultures: string[]) {
  const alternates = new Map<string, string>();

  for (const culture of supportedCultures) {
    const link = document.querySelector<HTMLLinkElement>(
      `link[rel="alternate"][hreflang="${culture}"]`,
    );

    if (!link?.href) {
      continue;
    }

    try {
      const url = new URL(link.href, window.location.origin);
      if (url.origin === window.location.origin) {
        alternates.set(culture, `${url.pathname}${url.search}${url.hash}`);
      }
    } catch {
      // Ignore malformed alternate URLs and fall back to the current route.
    }
  }

  return alternates;
}

function buildCultureHref(
  pathname: string,
  searchParams: URLSearchParams,
  culture: string,
  languageAlternates: Map<string, string>,
) {
  const params = cloneSearchParams(searchParams);
  params.delete(INFERRED_CULTURE_SEARCH_PARAM);
  const strippedPath = stripCulturePrefix(pathname).pathname;
  const alternatePath = languageAlternates.get(culture);
  const targetPath = alternatePath
    ? stripCulturePrefix(alternatePath).pathname
    : strippedPath;

  if (isPublicLocalizedPath(targetPath)) {
    params.set("culture", culture);
    const query = params.toString();
    return query ? `${targetPath}?${query}` : targetPath;
  }

  params.set("culture", culture);
  const query = params.toString();
  return query ? `${pathname}?${query}` : pathname;
}

export function CultureSwitcher({
  currentCulture,
  supportedCultures,
  languageAlternates: serverLanguageAlternates,
}: CultureSwitcherProps) {
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [languageAlternates, setLanguageAlternates] = useState<
    Map<string, string>
  >(() => toLanguageAlternateMap(serverLanguageAlternates));

  useEffect(() => {
    setLanguageAlternates(
      new Map([
        ...toLanguageAlternateMap(serverLanguageAlternates),
        ...readDocumentLanguageAlternates(supportedCultures),
      ]),
    );
  }, [pathname, serverLanguageAlternates, supportedCultures]);

  return (
    <div className="flex flex-wrap items-center gap-2">
      {supportedCultures.map((culture) => {
        const isActive = culture === currentCulture;

        return (
          <Link
            key={culture}
            href={buildCultureHref(
              pathname,
              searchParams,
              culture,
              languageAlternates,
            )}
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
