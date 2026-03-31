import "server-only";
import { getPublicMenuByName } from "@/features/cms/api/public-cms";
import type { PublicMenuItem } from "@/features/cms/types";
import { fallbackFooterGroups, fallbackPrimaryNavigation, utilityLinks } from "@/features/shell/navigation";
import type { ShellLink, ShellModel } from "@/features/shell/types";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import { activeTheme } from "@/themes/registry";

function sortMenuItems(items: PublicMenuItem[]) {
  return [...items].sort((left, right) => left.sortOrder - right.sortOrder);
}

function mapMenuItemsToLinks(items: PublicMenuItem[]): ShellLink[] {
  return sortMenuItems(items)
    .filter((item) => !item.parentId && item.url)
    .map((item) => ({
      label: item.label,
      href: item.url,
    }));
}

export async function getShellModel(): Promise<ShellModel> {
  const runtimeConfig = getSiteRuntimeConfig();
  const menuResult = await getPublicMenuByName(runtimeConfig.mainMenuName);
  const cmsLinks = menuResult.data
    ? mapMenuItemsToLinks(menuResult.data.items)
    : [];
  const primaryNavigation = cmsLinks.length > 0
    ? cmsLinks
    : fallbackPrimaryNavigation;
  const menuStatus = cmsLinks.length > 0
    ? "ok"
    : menuResult.status === "ok"
      ? "empty-menu"
      : menuResult.status;
  const menuMessage = cmsLinks.length > 0
    ? undefined
    : menuResult.status === "ok"
      ? `Public CMS menu "${runtimeConfig.mainMenuName}" returned no top-level links.`
    : menuResult.status === "not-found"
      ? `Public CMS menu "${runtimeConfig.mainMenuName}" was not found.`
      : menuResult.message;

  return {
    activeThemeName: activeTheme.displayName,
    culture: runtimeConfig.culture,
    menuSource: cmsLinks.length > 0 ? "cms" : "fallback",
    menuStatus,
    menuMessage,
    primaryNavigation,
    utilityLinks,
    footerGroups: fallbackFooterGroups,
  };
}
