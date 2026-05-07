export type ShellLink = {
  label: string;
  href: string;
};

export type ShellLinkGroup = {
  title: string;
  links: ShellLink[];
};

export type ShellModel = {
  activeThemeName: string;
  culture: string;
  supportedCultures: string[];
  menuSource: "cms" | "fallback";
  menuStatus:
    | "ok"
    | "empty-menu"
    | "not-found"
    | "network-error"
    | "http-error"
    | "invalid-payload";
  menuMessage?: string;
  primaryNavigation: ShellLink[];
  utilityLinks: ShellLink[];
  footerGroups: ShellLinkGroup[];
  footerColumnCount: number;
};
