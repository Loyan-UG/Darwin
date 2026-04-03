import "server-only";
import {
  getFallbackFooterGroups,
  getFallbackPrimaryNavigation,
  getUtilityLinks,
} from "@/features/shell/navigation";
import { getShellContext } from "@/features/shell/server/get-shell-context";
import { resolveShellMenu } from "@/features/shell/shell-menu";
import type { ShellLink, ShellModel } from "@/features/shell/types";
import { formatResource, getSharedResource } from "@/localization";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import { localizeHref, sanitizeAppPath } from "@/lib/locale-routing";
import { summarizeShellModelHealth } from "@/lib/route-health";
import { shellObservationContext } from "@/lib/route-observation-context";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import { toSafeHttpUrl } from "@/lib/webapi-url";
import { resolveTheme } from "@/themes/registry";

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
    const activeTheme = resolveTheme(runtimeConfig.theme);
    const menuResult = await getShellContext(runtimeConfig.mainMenuName);
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

    return {
      activeThemeName: activeTheme.displayName,
      culture,
      supportedCultures: runtimeConfig.supportedCultures,
      menuSource: menu.menuSource,
      menuStatus: menu.menuStatus,
      menuMessage: menu.menuMessage,
      primaryNavigation: menu.primaryNavigation,
      utilityLinks: localizeShellLinks(getUtilityLinks(culture), culture),
      footerGroups: getFallbackFooterGroups(culture).map((group) => ({
        ...group,
        links: localizeShellLinks(group.links, culture),
      })),
    };
  },
});
