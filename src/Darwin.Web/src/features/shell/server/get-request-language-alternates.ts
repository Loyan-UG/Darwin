import "server-only";
import { headers } from "next/headers";
import { getLocalizedPublicDiscoveryInventory } from "@/features/storefront/server/get-localized-public-discovery-inventory";
import {
  REQUEST_PATHNAME_HEADER,
  stripCulturePrefix,
} from "@/lib/locale-routing";
import { canonicalizeLanguageAlternates } from "@/lib/localized-alternates";
import { buildStablePublicLanguageAlternates } from "@/lib/seo";

function normalizePath(pathname: string | null) {
  const strippedPath = stripCulturePrefix(pathname ?? "/").pathname;
  return strippedPath !== "/" && strippedPath.endsWith("/")
    ? strippedPath.slice(0, -1)
    : strippedPath;
}

function isIndexLevelPublicPath(pathname: string) {
  return pathname === "/" || pathname === "/help" || pathname === "/catalog";
}

function findAlternatesForPath(
  alternatesById: Map<string, Record<string, string>>,
  pathname: string,
) {
  for (const alternates of alternatesById.values()) {
    const normalizedAlternates = canonicalizeLanguageAlternates(alternates);
    if (!normalizedAlternates) {
      continue;
    }

    if (
      Object.values(normalizedAlternates).some(
        (alternatePath) => normalizePath(alternatePath) === pathname,
      )
    ) {
      return normalizedAlternates;
    }
  }

  return undefined;
}

export async function getRequestLanguageAlternates() {
  const headerStore = await headers();
  const pathname = normalizePath(headerStore.get(REQUEST_PATHNAME_HEADER));

  if (isIndexLevelPublicPath(pathname)) {
    return buildStablePublicLanguageAlternates(pathname);
  }

  if (
    !pathname.startsWith("/cms/") &&
    !pathname.startsWith("/page/") &&
    !pathname.startsWith("/catalog/")
  ) {
    return undefined;
  }

  const inventory = await getLocalizedPublicDiscoveryInventory();

  return pathname.startsWith("/cms/") || pathname.startsWith("/page/")
    ? findAlternatesForPath(inventory.pageAlternatesById, pathname)
    : findAlternatesForPath(inventory.productAlternatesById, pathname);
}
