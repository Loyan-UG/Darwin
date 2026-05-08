import "server-only";
import {
  getFallbackFooterGroups,
  getFallbackPrimaryNavigation,
  getUtilityLinks,
} from "@/features/shell/navigation";
import { getShellContext } from "@/features/shell/server/get-shell-context";
import { resolveShellMenu } from "@/features/shell/shell-menu";
import { mapMenuItemsToLinks } from "@/features/shell/shell-menu";
import type { ShellLink, ShellModel } from "@/features/shell/types";
import { formatResource, getSharedResource, getShellResource } from "@/localization";
import { buildCatalogProductPath, buildCmsPagePath } from "@/lib/entity-paths";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { localizeHref, sanitizeAppPath, stripCulturePrefix } from "@/lib/locale-routing";
import { summarizeShellModelHealth } from "@/lib/route-health";
import { shellObservationContext } from "@/lib/route-observation-context";
import { getPublicSiteRuntimeConfig } from "@/lib/public-site-runtime-config";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import { toSafeHttpUrl } from "@/lib/webapi-url";
import { resolveTheme } from "@/themes/registry";

const legacyCmsSlugs = new Set([
  "ueber-uns",
  "kontakt",
  "impressum",
  "datenschutz",
  "agb",
  "versand",
  "rueckgabe",
  "faq",
  "zahlung",
  "reparatur-service",
  "garantie",
  "filialen",
  "jobs",
  "news",
  "marken",
  "kundenkonto",
  "widerruf",
  "datensicherheit",
  "lieferstatus",
  "geschenkkarten",
]);

function normalizeLegacySeedHref(rawHref: string) {
  const hashIndex = rawHref.indexOf("#");
  const hash = hashIndex >= 0 ? rawHref.slice(hashIndex) : "";
  const beforeHash = hashIndex >= 0 ? rawHref.slice(0, hashIndex) : rawHref;
  const queryIndex = beforeHash.indexOf("?");
  const search = queryIndex >= 0 ? beforeHash.slice(queryIndex) : "";
  const pathnameInput = queryIndex >= 0
    ? beforeHash.slice(0, queryIndex)
    : beforeHash;
  const normalized = stripCulturePrefix(pathnameInput);
  const pathname = normalized.pathname;
  const suffix = `${search}${hash}`;

  if (pathname === "/home") {
    return suffix ? `/${suffix}` : "/";
  }

  if (pathname === "/c") {
    return `/catalog${suffix}`;
  }

  if (pathname.startsWith("/c/")) {
    return `${buildCatalogProductPath(pathname.slice(3))}${suffix}`;
  }

  if (pathname === "/cms" || pathname === "/cms/help" || pathname === "/cms/faq") {
    return `/help${suffix}`;
  }

  if (pathname.startsWith("/cms/")) {
    return `${buildCmsPagePath(pathname.slice(5))}${suffix}`;
  }

  const slug = pathname.startsWith("/") ? pathname.slice(1) : pathname;
  if (legacyCmsSlugs.has(slug)) {
    return `${buildCmsPagePath(slug)}${suffix}`;
  }

  return rawHref;
}

function normalizeShellHref(rawHref: string) {
  const trimmed = normalizeLegacySeedHref(rawHref.trim());
  if (!trimmed) {
    return null;
  }

  if (trimmed.startsWith("/")) {
    const sanitized = sanitizeAppPath(trimmed, "/");
    return sanitized === "/" && trimmed !== "/" ? null : sanitized;
  }

  return toSafeHttpUrl(trimmed);
}

function localizeShellLinks(links: ShellLink[], culture: string) {
  return links.map((link) => ({
    ...link,
    href: localizeHref(link.href, culture),
  }));
}

function dedupeFooterGroups(groups: { title: string; links: ShellLink[] }[]) {
  const seen = new Set<string>();

  return groups
    .map((group) => ({
      ...group,
      links: group.links.filter((link) => {
        const key = `${group.title}:${link.href}`;
        if (seen.has(key)) {
          return false;
        }

        seen.add(key);
        return true;
      }),
    }))
    .filter((group) => group.links.length > 0);
}

function getEmptyMenuMessage(culture: string, menuName: string) {
  const shared = getSharedResource(culture);
  return formatResource(shared.menuMessages.emptyMenu, { menuName });
}

function getNotFoundMenuMessage(culture: string, menuName: string) {
  const shared = getSharedResource(culture);
  return formatResource(shared.menuMessages.notFound, { menuName });
}

export const getShellModel = createCachedObservedLoader({
  area: "shell-model",
  operation: "load-shell-model",
  thresholdMs: 250,
  getContext: (culture: string) => ({
    ...shellObservationContext(getSiteRuntimeConfig().mainMenuName),
    culture,
  }),
  getSuccessContext: summarizeShellModelHealth,
  load: async (culture: string): Promise<ShellModel> => {
    const runtimeConfig = getSiteRuntimeConfig();
    const publicRuntimeConfig = await getPublicSiteRuntimeConfig();
    const activeTheme = resolveTheme(runtimeConfig.theme);
    const [menuResult, footerResult] = await Promise.all([
      getShellContext(culture, runtimeConfig.mainMenuName),
      getShellContext(culture, runtimeConfig.footerMenuName),
    ]);
    const menu = resolveShellMenu({
      culture,
      menuName: runtimeConfig.mainMenuName,
      menuResultStatus: menuResult.status,
      menuResultMessage: menuResult.message,
      menuItems: menuResult.data?.items,
      fallbackLinks: getFallbackPrimaryNavigation(culture),
      localizeLink: localizeHref,
      normalizeHref: normalizeShellHref,
      formatEmptyMenuMessage: (menuName) => getEmptyMenuMessage(culture, menuName),
      formatNotFoundMenuMessage: (menuName) =>
        getNotFoundMenuMessage(culture, menuName),
    });
    const fallbackFooterGroups = getFallbackFooterGroups(culture).map((group) => ({
      ...group,
      links: localizeShellLinks(group.links, culture),
    }));
    const footerLinks = footerResult.data?.items
      ? localizeShellLinks(
          mapMenuItemsToLinks(footerResult.data.items, normalizeShellHref),
          culture,
        )
      : [];
    const footerGroups = dedupeFooterGroups(
      footerLinks.length > 0
        ? [
            {
              title: getShellResource(culture).footerNavigationTitle,
              links: footerLinks,
            },
          ]
        : fallbackFooterGroups,
    );

    return {
      activeThemeName: activeTheme.displayName,
      culture,
      supportedCultures: publicRuntimeConfig.supportedCultures,
      menuSource: menu.menuSource,
      menuStatus: menu.menuStatus,
      menuMessage: menu.menuMessage,
      primaryNavigation: menu.primaryNavigation,
      utilityLinks: localizeShellLinks(getUtilityLinks(culture), culture),
      footerGroups,
      footerColumnCount: runtimeConfig.footerColumnCount,
    };
  },
});
