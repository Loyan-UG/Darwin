import "server-only";
import { getPublicMenuByName } from "@/features/cms/api/public-cms";
import type { PublicMenuItem } from "@/features/cms/types";
import {
  getFallbackFooterGroups,
  getFallbackPrimaryNavigation,
  getUtilityLinks,
} from "@/features/shell/navigation";
import type { ShellLink, ShellModel } from "@/features/shell/types";
import { formatResource, getSharedResource } from "@/localization";
import { localizeHref, sanitizeAppPath } from "@/lib/locale-routing";
import { getRequestCulture } from "@/lib/request-culture";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import { toSafeHttpUrl } from "@/lib/webapi-url";
import { activeTheme } from "@/themes/registry";

function sortMenuItems(items: PublicMenuItem[]) {
  return [...items].sort((left, right) => left.sortOrder - right.sortOrder);
}

function mapMenuItemsToLinks(items: PublicMenuItem[]): ShellLink[] {
  return sortMenuItems(items)
    .filter((item) => !item.parentId && item.url)
    .flatMap((item) => {
      const href = normalizeShellHref(item.url);
      return href
        ? [{
            label: item.label,
            href,
          }]
        : [];
    });
}

function normalizeShellHref(rawHref: string) {
  const trimmed = rawHref.trim();
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

function getMenuMessage(
  culture: string,
  menuName: string,
  menuStatus: string,
  menuResultMessage?: string,
) {
  const shared = getSharedResource(culture);

  if (menuStatus === "ok") {
    return formatResource(shared.menuMessages.emptyMenu, { menuName });
  }

  if (menuStatus === "not-found") {
    return formatResource(shared.menuMessages.notFound, { menuName });
  }

  return menuResultMessage;
}

export async function getShellModel(): Promise<ShellModel> {
  const runtimeConfig = getSiteRuntimeConfig();
  const culture = await getRequestCulture();
  const menuResult = await getPublicMenuByName(runtimeConfig.mainMenuName);
  const cmsLinks = menuResult.data
    ? localizeShellLinks(mapMenuItemsToLinks(menuResult.data.items), culture)
    : [];
  const primaryNavigation = cmsLinks.length > 0
    ? cmsLinks
    : localizeShellLinks(getFallbackPrimaryNavigation(culture), culture);
  const menuStatus = cmsLinks.length > 0
    ? "ok"
    : menuResult.status === "ok"
      ? "empty-menu"
      : menuResult.status;
  const menuMessage = cmsLinks.length > 0
    ? undefined
    : getMenuMessage(
        culture,
        runtimeConfig.mainMenuName,
        menuResult.status,
        menuResult.message,
      );

  return {
    activeThemeName: activeTheme.displayName,
    culture,
    supportedCultures: runtimeConfig.supportedCultures,
    menuSource: cmsLinks.length > 0 ? "cms" : "fallback",
    menuStatus,
    menuMessage,
    primaryNavigation,
    utilityLinks: localizeShellLinks(getUtilityLinks(culture), culture),
    footerGroups: getFallbackFooterGroups(culture).map((group) => ({
      ...group,
      links: localizeShellLinks(group.links, culture),
    })),
  };
}
