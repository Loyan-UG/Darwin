import type { ShellLink, ShellLinkGroup } from "@/features/shell/types";

export const fallbackPrimaryNavigation: ShellLink[] = [
  { label: "Home", href: "/" },
  { label: "Catalog", href: "/catalog" },
  { label: "Account", href: "/account" },
  { label: "Loyalty", href: "/loyalty" },
  { label: "Orders", href: "/orders" },
  { label: "Invoices", href: "/invoices" },
];

export const utilityLinks: ShellLink[] = [
  { label: "Open storefront", href: "/catalog" },
  { label: "Cart", href: "/cart" },
  { label: "Checkout", href: "/checkout" },
];

export const fallbackFooterGroups: ShellLinkGroup[] = [
  {
    title: "Storefront",
    links: [
      { label: "Catalog", href: "/catalog" },
      { label: "Cart", href: "/cart" },
      { label: "Checkout", href: "/checkout" },
      { label: "Home", href: "/" },
      { label: "Loyalty preview", href: "/loyalty" },
    ],
  },
  {
    title: "Member",
    links: [
      { label: "Account", href: "/account" },
      { label: "Loyalty", href: "/loyalty" },
      { label: "Orders", href: "/orders" },
      { label: "Invoices", href: "/invoices" },
    ],
  },
  {
    title: "Platform",
    links: [
      { label: "Theme foundation", href: "/" },
      { label: "Config-driven behavior", href: "/account" },
      { label: "Commerce scaffolding", href: "/orders" },
    ],
  },
];
