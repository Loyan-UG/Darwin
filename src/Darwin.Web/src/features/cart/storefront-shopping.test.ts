import test from "node:test";
import assert from "node:assert/strict";
import type { CartDisplaySnapshot } from "@/features/cart/types";
import { extractCartLinkedProductSlugs } from "@/features/cart/storefront-shopping";

function createSnapshot(
  overrides: Partial<CartDisplaySnapshot>,
): CartDisplaySnapshot {
  return {
    variantId: overrides.variantId ?? "variant-1",
    name: overrides.name ?? "Sample product",
    href: overrides.href ?? "/catalog/sample-product",
    imageUrl: overrides.imageUrl ?? null,
    imageAlt: overrides.imageAlt ?? null,
    sku: overrides.sku ?? null,
  };
}

test("extractCartLinkedProductSlugs keeps unique catalog slugs in snapshot order", () => {
  const snapshots = [
    createSnapshot({
      variantId: "a",
      href: "/catalog/apples",
    }),
    createSnapshot({
      variantId: "b",
      href: "/catalog/apples?coupon=spring",
    }),
    createSnapshot({
      variantId: "c",
      href: "/catalog/bananas#hero",
    }),
  ];

  assert.deepEqual(extractCartLinkedProductSlugs(snapshots), [
    "apples",
    "bananas",
  ]);
});

test("extractCartLinkedProductSlugs ignores non-catalog paths", () => {
  const snapshots = [
    createSnapshot({
      variantId: "a",
      href: "/cart",
    }),
    createSnapshot({
      variantId: "b",
      href: "/cms/about",
    }),
    createSnapshot({
      variantId: "c",
      href: "/catalog/cherries",
    }),
  ];

  assert.deepEqual(extractCartLinkedProductSlugs(snapshots), ["cherries"]);
});

test("extractCartLinkedProductSlugs ignores unsafe or external snapshot href values", () => {
  const snapshots = [
    createSnapshot({
      variantId: "a",
      href: "https://evil.example/catalog/apples",
    }),
    createSnapshot({
      variantId: "b",
      href: "//evil.example/catalog/bananas",
    }),
    createSnapshot({
      variantId: "c",
      href: "javascript:alert(1)",
    }),
    createSnapshot({
      variantId: "d",
      href: "/catalog/dates",
    }),
  ];

  assert.deepEqual(extractCartLinkedProductSlugs(snapshots), ["dates"]);
});
