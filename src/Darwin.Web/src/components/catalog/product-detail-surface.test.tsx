import assert from "node:assert/strict";
import fs from "node:fs";
import Module from "node:module";
import os from "node:os";
import path from "node:path";
import test from "node:test";
import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import type {
  PublicCategorySummary,
  PublicProductDetail,
  PublicProductSummary,
} from "@/features/catalog/types";

const stubDirectory = fs.mkdtempSync(path.join(os.tmpdir(), "darwin-product-detail-surface-"));
const serverOnlyStubPath = path.join(stubDirectory, "server-only.js");
fs.writeFileSync(serverOnlyStubPath, "module.exports = {};\n", "utf8");

const originalResolveFilename = Module._resolveFilename;
Module._resolveFilename = function patchedResolveFilename(
  request,
  parent,
  isMain,
  options,
) {
  if (request === "server-only") {
    return serverOnlyStubPath;
  }

  return originalResolveFilename.call(this, request, parent, isMain, options);
};

const category: PublicCategorySummary = {
  id: "category-1",
  parentId: null,
  slug: "fruit",
  name: "Fruit",
  description: "Fresh produce aisle",
  sortOrder: 1,
};

const product: PublicProductDetail = {
  id: "product-1",
  slug: "apples",
  name: "Apples",
  shortDescription: "Crisp apples for the weekly basket.",
  currency: "EUR",
  priceMinor: 700,
  compareAtPriceMinor: 1000,
  primaryImageUrl: "/media/apples-primary.png",
  fullDescriptionHtml: "<p>Seasonal apples packed for daily grocery runs.</p>",
  metaTitle: "Apples",
  metaDescription: "Fresh apple detail page",
  primaryCategoryId: "category-1",
  variants: [
    {
      id: "variant-1",
      sku: "APL-1",
      currency: "EUR",
      basePriceNetMinor: 700,
      compareAtPriceNetMinor: 1000,
      backorderAllowed: true,
      isDigital: false,
    },
  ],
  media: [
    {
      id: "media-1",
      url: "/media/apples-1.png",
      alt: "Apples",
      title: "Apples",
      role: "gallery",
      sortOrder: 1,
    },
  ],
};

const relatedProducts: PublicProductSummary[] = [
  {
    id: "product-2",
    slug: "pears",
    name: "Pears",
    shortDescription: "Companion fruit pick.",
    currency: "EUR",
    priceMinor: 650,
    compareAtPriceMinor: 850,
    primaryImageUrl: "/media/pears.png",
  },
];

test("ProductDetailPage renders the upgraded grocery detail surface directly", async () => {
  const { ProductDetailPage } = await import("@/components/catalog/product-detail-page");
  const html = renderToStaticMarkup(
    React.createElement(ProductDetailPage, {
      culture: "en-US",
      product,
      primaryCategory: category,
      relatedProducts,
      status: "ok",
    }),
  );

  assert.match(
    html,
    /linear-gradient\(135deg,#f6ffe9_0%,#ffffff_38%,#fff1d2_100%\)/,
  );
  assert.match(html, /Apples/);
  assert.doesNotMatch(html, /Review queue/);
  assert.match(html, /Related products/);
  assert.ok(
    html.includes('href="/en-US/cart"') || html.includes('href="/cart"'),
  );
  assert.ok(
    html.includes('href="/en-US/catalog/pears"') ||
      html.includes('href="/catalog/pears"'),
  );
});
