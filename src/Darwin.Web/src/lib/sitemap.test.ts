import test from "node:test";
import assert from "node:assert/strict";
import {
  groupLocalizedDetailAlternates,
  mapLocalizedDetailAlternatesById,
} from "@/lib/sitemap-helpers";

test("groupLocalizedDetailAlternates prefers the default-culture path and keeps localized alternates together", () => {
  const entries = groupLocalizedDetailAlternates(
    [
      {
        culture: "de-DE",
        items: [
          { id: "page-1", slug: "ueber-uns" },
          { id: "page-2", slug: "faq" },
        ],
      },
      {
        culture: "en-US",
        items: [
          { id: "page-1", slug: "about-us" },
          { id: "page-2", slug: "faq" },
        ],
      },
    ],
    (slug) => `/page/${slug}`,
  );

  assert.deepEqual(entries, [
    {
      path: "/page/ueber-uns",
      languageAlternates: {
        "de-DE": "/page/ueber-uns",
        "en-US": "/page/about-us",
      },
    },
    {
      path: "/page/faq",
      languageAlternates: {
        "de-DE": "/page/faq",
        "en-US": "/page/faq",
      },
    },
  ]);
});

test("groupLocalizedDetailAlternates keeps entries when only one culture currently exposes the item", () => {
  const entries = groupLocalizedDetailAlternates(
    [
      {
        culture: "de-DE",
        items: [{ id: "product-1", slug: "kaffee" }],
      },
      {
        culture: "en-US",
        items: [],
      },
    ],
    (slug) => `/catalog/${slug}`,
  );

  assert.deepEqual(entries, [
    {
      path: "/catalog/kaffee",
      languageAlternates: {
        "de-DE": "/catalog/kaffee",
      },
    },
  ]);
});

test("mapLocalizedDetailAlternatesById keeps canonical localized paths keyed by content id", () => {
  const alternatesById = mapLocalizedDetailAlternatesById(
    [
      {
        culture: "de-DE",
        items: [{ id: "product-1", slug: "kaffee" }],
      },
      {
        culture: "en-US",
        items: [{ id: "product-1", slug: "coffee" }],
      },
    ],
    (slug) => `/catalog/${slug}`,
  );

  assert.deepEqual(alternatesById.get("product-1"), {
    "de-DE": "/catalog/kaffee",
    "en-US": "/catalog/coffee",
  });
});
