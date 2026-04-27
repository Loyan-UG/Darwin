import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";
import React from "react";
import Module from "node:module";
import { renderToStaticMarkup } from "react-dom/server";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicCartSummary } from "@/features/cart/types";
import type { PublicPageSummary } from "@/features/cms/types";

const category: PublicCategorySummary = {
  id: "category-1",
  slug: "fruit",
  name: "Fruit",
  description: "Fresh produce aisle",
  productCount: 8,
};

const product: PublicProductSummary = {
  id: "product-1",
  slug: "apples",
  name: "Apples",
  priceMinor: 700,
  compareAtPriceMinor: 1000,
  currency: "EUR",
  imageUrl: null,
  primaryImageUrl: null,
  shortDescription: "Crisp apples",
  categoryName: "Fruit",
};

const cmsPage: PublicPageSummary = {
  id: "cms-1",
  slug: "herb-guide",
  title: "Herb guide",
  metaDescription: "Storage tips for herbs",
};

const stubDirectory = fs.mkdtempSync(path.join(os.tmpdir(), "darwin-account-server-only-"));
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

const storefrontCart: PublicCartSummary = {
  id: "cart-1",
  currency: "EUR",
  subtotalNetMinor: 1200,
  subtotalGrossMinor: 1400,
  grandTotalGrossMinor: 1400,
  items: [
    {
      lineId: "line-1",
      quantity: 2,
      productId: "product-1",
      variantId: "variant-1",
      productName: "Apples",
      sku: "APL-1",
      unitPriceGrossMinor: 700,
      lineTotalGrossMinor: 1400,
      imageUrl: null,
      display: {
        href: "/catalog/apples",
      },
    },
  ],
};

test("AccountHubPage renders the upgraded grocery account entry surface", async () => {
  const { AccountHubPage } = await import("@/components/account/account-hub-page");
  const html = renderToStaticMarkup(
    React.createElement(AccountHubPage, {
      culture: "en-US",
      cmsPages: [cmsPage],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [product],
      productsStatus: "ok",
      storefrontCart,
      storefrontCartStatus: "ok",
      returnPath: "/checkout",
    }),
  );

  assert.match(
    html,
    /linear-gradient\(135deg,#f5ffe8_0%,#ffffff_42%,#fff1d0_100%\)/,
  );
  assert.match(html, /Next storefront moves/);
  assert.match(html, /Offer board/);
  assert.match(html, /Promotion lanes/);
  assert.ok(html.includes('href="/cart"') || html.includes('href="/en-US/cart"'));
  assert.ok(
    html.includes('href="/en-US/catalog/apples"') ||
      html.includes('href="/catalog/apples"'),
  );
});
