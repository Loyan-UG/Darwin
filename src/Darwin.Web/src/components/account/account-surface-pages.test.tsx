import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";
import React from "react";
import Module from "node:module";
import { renderToStaticMarkup } from "react-dom/server";
import type { PublicCartSummary } from "@/features/cart/types";

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
      storefrontCart,
      returnPath: "/checkout",
    }),
  );

  assert.match(
    html,
    /linear-gradient\(135deg,#f5ffe8_0%,#ffffff_42%,#fff1d0_100%\)/,
  );
  assert.match(html, /Your account/);
  assert.match(html, /Cart continuation/);
  assert.ok(html.includes('href="/cart"') || html.includes('href="/en-US/cart"'));
  assert.doesNotMatch(html, /Offer board/);
  assert.doesNotMatch(html, /Promotion lanes/);
});
