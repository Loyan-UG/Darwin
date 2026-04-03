import type { PublicMenuItem } from "@/features/cms/types";
import type { ShellLink, ShellModel } from "@/features/shell/types";

type ResolveShellMenuInput = {
  culture: string;
  menuName: string;
  menuResultStatus: "ok" | "not-found" | "network-error" | "http-error" | "invalid-payload";
  menuResultMessage?: string;
  menuItems?: PublicMenuItem[] | null;
  fallbackLinks: ShellLink[];
  localizeLink: (href: string, culture: string) => string;
  normalizeHref: (rawHref: string) => string | null;
  formatEmptyMenuMessage: (menuName: string) => string;
  formatNotFoundMenuMessage: (menuName: string) => string;
};

export function sortMenuItems(items: PublicMenuItem[]) {
  return [...items].sort((left, right) => left.sortOrder - right.sortOrder);
}

export function mapMenuItemsToLinks(
  items: PublicMenuItem[],
  normalizeHref: (rawHref: string) => string | null,
): ShellLink[] {
  return sortMenuItems(items)
    .filter((item) => !item.parentId && item.url)
    .flatMap((item) => {
      const href = normalizeHref(item.url);
      return href
        ? [{
            label: item.label,
            href,
          }]
        : [];
    });
}

export function localizeShellLinks(
  links: ShellLink[],
  culture: string,
  localizeLink: (href: string, culture: string) => string,
) {
  return links.map((link) => ({
    ...link,
    href: localizeLink(link.href, culture),
  }));
}

export function resolveShellMenu(input: ResolveShellMenuInput) {
  const cmsLinks = input.menuItems
    ? localizeShellLinks(
        mapMenuItemsToLinks(input.menuItems, input.normalizeHref),
        input.culture,
        input.localizeLink,
      )
    : [];
  const primaryNavigation = cmsLinks.length > 0
    ? cmsLinks
    : localizeShellLinks(
        input.fallbackLinks,
        input.culture,
        input.localizeLink,
      );
  const menuSource: ShellModel["menuSource"] = cmsLinks.length > 0
    ? "cms"
    : "fallback";
  const menuStatus: ShellModel["menuStatus"] = cmsLinks.length > 0
    ? "ok"
    : input.menuResultStatus === "ok"
      ? "empty-menu"
      : input.menuResultStatus;
  const menuMessage = cmsLinks.length > 0
    ? undefined
    : getMenuMessage(
        input.menuName,
        menuStatus,
        input.menuResultMessage,
        input.formatEmptyMenuMessage,
        input.formatNotFoundMenuMessage,
      );

  return {
    cmsLinks,
    primaryNavigation,
    menuSource,
    menuStatus,
    menuMessage,
  };
}

function getMenuMessage(
  menuName: string,
  menuStatus: ShellModel["menuStatus"],
  menuResultMessage: string | undefined,
  formatEmptyMenuMessage: (menuName: string) => string,
  formatNotFoundMenuMessage: (menuName: string) => string,
) {
  if (menuStatus === "empty-menu") {
    return formatEmptyMenuMessage(menuName);
  }

  if (menuStatus === "not-found") {
    return formatNotFoundMenuMessage(menuName);
  }

  return menuResultMessage;
}
