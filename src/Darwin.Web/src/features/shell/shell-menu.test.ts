import test from "node:test";
import assert from "node:assert/strict";
import type { PublicMenuItem } from "@/features/cms/types";
import {
  filterCustomerPrimaryNavigation,
  mapMenuItemsToLinks,
  resolveShellMenu,
  sortMenuItems,
} from "@/features/shell/shell-menu";

test("mapMenuItemsToLinks keeps only top-level items with valid hrefs in sort order", () => {
  const items: PublicMenuItem[] = [
    {
      id: "child",
      parentId: "about",
      label: "Nested child",
      url: "/nested",
      sortOrder: 1,
    },
    {
      id: "about",
      parentId: null,
      label: "About",
      url: "/about",
      sortOrder: 3,
    },
    {
      id: "external",
      parentId: null,
      label: "Partner",
      url: "https://example.com",
      sortOrder: 2,
    },
    {
      id: "bad",
      parentId: null,
      label: "Bad",
      url: "javascript:alert(1)",
      sortOrder: 4,
    },
  ];

  const links = mapMenuItemsToLinks(items, (href) =>
    href.startsWith("javascript:") ? null : href
  );

  assert.deepEqual(links, [
    {
      label: "Partner",
      href: "https://example.com",
    },
    {
      label: "About",
      href: "/about",
    },
  ]);
});

test("resolveShellMenu falls back to localized fallback navigation when cms menu is empty", () => {
  const result = resolveShellMenu({
    culture: "de-DE",
    menuName: "main-navigation",
    menuResultStatus: "ok",
    menuItems: [],
    fallbackLinks: [
      {
        label: "Catalog",
        href: "/catalog",
      },
    ],
    localizeLink: (href, culture) => `${culture}:${href}`,
    normalizeHref: (href) => href,
    formatEmptyMenuMessage: (menuName) => `empty:${menuName}`,
    formatNotFoundMenuMessage: (menuName) => `missing:${menuName}`,
  });

  assert.equal(result.menuSource, "fallback");
  assert.equal(result.menuStatus, "empty-menu");
  assert.equal(result.menuMessage, "empty:main-navigation");
  assert.deepEqual(result.primaryNavigation, [
    {
      label: "Catalog",
      href: "de-DE:/catalog",
    },
  ]);
});

test("resolveShellMenu prefers localized cms links when valid menu items exist", () => {
  const result = resolveShellMenu({
    culture: "en-US",
    menuName: "main-navigation",
    menuResultStatus: "not-found",
    menuItems: [
      {
        id: "home",
        parentId: null,
        label: "Home",
        url: "/",
        sortOrder: 1,
      },
      {
        id: "hidden",
        parentId: null,
        label: "Bad",
        url: "javascript:alert(1)",
        sortOrder: 2,
      },
      {
        id: "checkout",
        parentId: null,
        label: "Checkout",
        url: "/checkout",
        sortOrder: 3,
      },
    ],
    fallbackLinks: [
      {
        label: "Fallback",
        href: "/catalog",
      },
    ],
    localizeLink: (href, culture) => `${culture}:${href}`,
    normalizeHref: (href) => href.startsWith("javascript:") ? null : href,
    formatEmptyMenuMessage: (menuName) => `empty:${menuName}`,
    formatNotFoundMenuMessage: (menuName) => `missing:${menuName}`,
  });

  assert.equal(result.menuSource, "cms");
  assert.equal(result.menuStatus, "ok");
  assert.equal(result.menuMessage, undefined);
  assert.deepEqual(result.primaryNavigation, [
    {
      label: "Home",
      href: "en-US:/",
    },
  ]);
});

test("sortMenuItems preserves stable ascending order by sortOrder", () => {
  const labels = sortMenuItems([
    {
      id: "3",
      parentId: null,
      label: "Third",
      url: "/third",
      sortOrder: 30,
    },
    {
      id: "1",
      parentId: null,
      label: "First",
      url: "/first",
      sortOrder: 10,
    },
    {
      id: "2",
      parentId: null,
      label: "Second",
      url: "/second",
      sortOrder: 20,
    },
  ]).map((item) => item.label);

  assert.deepEqual(labels, ["First", "Second", "Third"]);
});

test("resolveShellMenu keeps richer fallback navigation discoverable when CMS menu is unavailable", () => {
  const result = resolveShellMenu({
    culture: "en-US",
    menuName: "main-navigation",
    menuResultStatus: "network-error",
    menuItems: null,
    fallbackLinks: [
      { label: "Home", href: "/" },
      { label: "Catalog", href: "/catalog" },
      { label: "Help", href: "/help" },
      { label: "Checkout", href: "/checkout" },
    ],
    localizeLink: (href, culture) => `${culture}:${href}`,
    normalizeHref: (href) => href,
    formatEmptyMenuMessage: (menuName) => `empty:${menuName}`,
    formatNotFoundMenuMessage: (menuName) => `missing:${menuName}`,
  });

  assert.equal(result.menuSource, "fallback");
  assert.equal(result.menuStatus, "network-error");
  assert.deepEqual(result.primaryNavigation, [
    { label: "Home", href: "en-US:/" },
    { label: "Catalog", href: "en-US:/catalog" },
    { label: "Help", href: "en-US:/help" },
  ]);
});

test("filterCustomerPrimaryNavigation keeps CMS-backed primary menus storefront-safe", () => {
  assert.deepEqual(
    filterCustomerPrimaryNavigation(
      [
        { label: "Home", href: "/" },
        { label: "Shop", href: "/catalog" },
        { label: "FAQ", href: "/cms/faq" },
        { label: "Contact", href: "/cms/contact" },
        { label: "CMS", href: "/cms" },
        { label: "Orders", href: "/orders" },
        { label: "Checkout", href: "/checkout" },
      ],
      "en-US",
    ),
    [
      { label: "Home", href: "/" },
      { label: "Shop", href: "/catalog" },
      { label: "Help", href: "/help" },
      { label: "Contact", href: "/page/contact" },
    ],
  );
});

