import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";
import React from "react";
import Module from "node:module";
import { renderToStaticMarkup } from "react-dom/server";
import type { PublicProductSummary } from "@/features/catalog/types";

const stubDirectory = fs.mkdtempSync(path.join(os.tmpdir(), "darwin-server-only-"));
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

const heroProduct: PublicProductSummary = {
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

test("CartPage renders the upgraded grocery basket surface", async () => {
  const { CartPage } = await import("@/components/cart/cart-page");
  const html = renderToStaticMarkup(
    React.createElement(CartPage, {
      culture: "en-US",
      model: {
        anonymousId: "anon-1",
        status: "ok",
        cart: {
          cartId: "cart-1",
          currency: "EUR",
          subtotalNetMinor: 1200,
          vatTotalMinor: 200,
          grandTotalGrossMinor: 1400,
          couponCode: "WELCOME10",
          items: [
            {
              variantId: "variant-1",
              quantity: 2,
              unitPriceNetMinor: 600,
              addOnPriceDeltaMinor: 0,
              vatRate: 0.19,
              lineNetMinor: 1200,
              lineVatMinor: 200,
              lineGrossMinor: 1400,
              selectedAddOnValueIdsJson: "[]",
              display: {
                variantId: "variant-1",
                name: "Apples",
                href: "/catalog/apples",
                imageUrl: null,
                imageAlt: "Apples",
                sku: "APL-1",
              },
            },
          ],
        },
      },
      memberAddresses: [],
      memberAddressesStatus: "unauthenticated",
      memberProfile: null,
      memberProfileStatus: "unauthenticated",
      memberPreferences: null,
      memberPreferencesStatus: "unauthenticated",
      hasMemberSession: false,
      cartStatus: "updated",
      followUpProducts: [heroProduct],
    }),
  );

  assert.match(
    html,
    /linear-gradient\(135deg,#f5ffe8_0%,#ffffff_42%,#fff1d0_100%\)/,
  );
  assert.match(html, /WELCOME10/);
  assert.match(html, /Continue shopping/);
  assert.match(html, /Grand total/);
  assert.ok(html.includes('href="/en-US/catalog/apples"'));
  assert.ok(html.includes('href="/en-US/checkout"') || html.includes('href="/checkout"'));
});

test("CheckoutPage renders the upgraded grocery conversion surface", async () => {
  const { CheckoutPage } = await import("@/components/checkout/checkout-page");
  const html = renderToStaticMarkup(
    React.createElement(CheckoutPage, {
      culture: "en-US",
      model: {
        anonymousId: "anon-1",
        status: "ok",
        message: undefined,
        cart: {
          id: "cart-1",
          currency: "EUR",
          couponCode: null,
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
        },
      },
      draft: {
        fullName: "Ada Lovelace",
        company: "",
        street1: "Main Street 1",
        street2: "",
        postalCode: "10115",
        city: "Berlin",
        state: "",
        countryCode: "DE",
        phoneE164: "+49123456789",
        shippingMethodId: "ship-1",
      },
      intent: {
        currency: "EUR",
        subtotalNetMinor: 1200,
        subtotalGrossMinor: 1400,
        discountTotalMinor: 0,
        shippingOptions: [
          {
            id: "ship-1",
            name: "Standard",
            description: "Standard shipping",
            totalMinor: 100,
          },
        ],
        selectedShippingMethodId: "ship-1",
        selectedShippingTotalMinor: 100,
        shippingCountryCode: "DE",
        shipmentMassGrams: 500,
        grandTotalGrossMinor: 1400,
      },
      intentStatus: "ok",
      memberAddresses: [],
      profilePrefillActive: false,
      hasMemberSession: false,
    }),
  );

  assert.match(
    html,
    /linear-gradient\(135deg,#f5ffe8_0%,#ffffff_42%,#fff1d0_100%\)/,
  );
  assert.match(html, /Payment continuity/);
  assert.match(html, /Estimated grand total/);
  assert.ok(html.includes('href="/en-US/cart"') || html.includes('href="/cart"'));
  assert.ok(
    html.includes('href="/en-US/account/sign-in"') ||
      html.includes('href="/account/sign-in"'),
  );
});
