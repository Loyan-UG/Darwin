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

function getLocalizedHelpLabel(culture: string) {
  return culture.toLowerCase().startsWith("de") ? "Hilfe" : "Help";
}

function normalizePrimaryNavigationLink(link: ShellLink, culture: string): ShellLink | null {
  const label = link.label.trim();
  const href = link.href.trim();
  const lowerLabel = label.toLowerCase();

  if (
    lowerLabel === "cms" ||
    lowerLabel === "checkout" ||
    lowerLabel === "orders" ||
    lowerLabel === "invoices"
  ) {
    return null;
  }

  if (href === "/cms/faq" || href === "/cms/help") {
    return {
      label: getLocalizedHelpLabel(culture),
      href: "/help",
    };
  }

  if (href === "/cms/contact") {
    return {
      label,
      href: "/page/contact",
    };
  }

  if (href === "/cms/kontakt") {
    return {
      label,
      href: "/page/kontakt",
    };
  }

  if (
    href === "/" ||
    href === "/catalog" ||
    href === "/loyalty" ||
    href === "/help" ||
    href === "/contact" ||
    href === "/page/contact" ||
    href === "/page/kontakt"
  ) {
    return {
      label,
      href,
    };
  }

  return null;
}

export function filterCustomerPrimaryNavigation(
  links: ShellLink[],
  culture: string,
) {
  return links.flatMap((link) => {
    const normalizedLink = normalizePrimaryNavigationLink(link, culture);
    return normalizedLink ? [normalizedLink] : [];
  });
}

export function resolveShellMenu(input: ResolveShellMenuInput) {
  const cmsLinks = input.menuItems
    ? localizeShellLinks(
        filterCustomerPrimaryNavigation(
          mapMenuItemsToLinks(input.menuItems, input.normalizeHref),
          input.culture,
        ),
        input.culture,
        input.localizeLink,
      )
    : [];
  const primaryNavigation = cmsLinks.length > 0
    ? cmsLinks
    : localizeShellLinks(
        filterCustomerPrimaryNavigation(input.fallbackLinks, input.culture),
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
