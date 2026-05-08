import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";
import React from "react";
import Module from "node:module";
import { renderToStaticMarkup } from "react-dom/server";
import type { PublicCartSummary } from "@/features/cart/types";

const stubDirectory = fs.mkdtempSync(path.join(os.tmpdir(), "darwin-member-dashboard-"));
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

test("MemberDashboardPage renders the upgraded grocery member dashboard surface", async () => {
  const { MemberDashboardPage } = await import("@/components/account/member-dashboard-page");
  const html = renderToStaticMarkup(
    React.createElement(MemberDashboardPage, {
      culture: "en-US",
      session: {
        email: "ada@example.com",
        isAuthenticated: true,
        accessTokenExpiresAtUtc: "2026-04-27T12:00:00Z",
      },
      profile: {
        id: "profile-1",
        email: "ada@example.com",
        firstName: "Ada",
        lastName: "Lovelace",
        phoneE164: "+49123456789",
        phoneNumberConfirmed: true,
        locale: "en-US",
        timezone: "Europe/Berlin",
        currency: "EUR",
        rowVersion: "rv-1",
      },
      profileStatus: "ok",
      preferences: {
        marketingConsent: true,
        allowEmailMarketing: true,
        allowSmsMarketing: false,
        allowWhatsAppMarketing: false,
        allowPromotionalPushNotifications: false,
        allowOptionalAnalyticsTracking: true,
      },
      preferencesStatus: "ok",
      customerContext: {
        displayName: "Ada Lovelace",
        email: "ada@example.com",
        companyName: null,
        interactionCount: 4,
        lastInteractionAtUtc: "2026-04-26T10:00:00Z",
        segments: [{ id: "segment-1", name: "VIP" }],
      },
      customerContextStatus: "ok",
      addresses: [
        {
          id: "address-1",
          rowVersion: "rv-1",
          fullName: "Ada Lovelace",
          company: null,
          street1: "Main Street 1",
          street2: null,
          postalCode: "10115",
          city: "Berlin",
          state: null,
          countryCode: "DE",
          phoneE164: "+49123456789",
          isDefaultBilling: true,
          isDefaultShipping: true,
        },
      ],
      addressesStatus: "ok",
      recentOrders: [
        {
          id: "order-1",
          orderNumber: "ORD-1001",
          currency: "EUR",
          grandTotalGrossMinor: 1400,
          status: "Processing",
          createdAtUtc: "2026-04-26T12:00:00Z",
        },
      ],
      recentOrdersStatus: "ok",
      recentInvoices: [
        {
          id: "invoice-1",
          orderNumber: "ORD-1001",
          currency: "EUR",
          totalGrossMinor: 1400,
          balanceMinor: 200,
          status: "Open",
          createdAtUtc: "2026-04-26T12:30:00Z",
        },
      ],
      recentInvoicesStatus: "ok",
      loyaltyOverview: {
        totalAccounts: 1,
        activeAccounts: 1,
        totalPointsBalance: 240,
        accounts: [
          {
            loyaltyAccountId: "loyalty-1",
            businessId: "biz-1",
            businessName: "Fresh Market",
            status: "Active",
            pointsBalance: 240,
            pointsToNextReward: 40,
            nextRewardTitle: "Free basket",
            lastAccrualAtUtc: "2026-04-24T09:00:00Z",
          },
        ],
      },
      loyaltyOverviewStatus: "ok",
      loyaltyBusinesses: [
        {
          businessId: "biz-1",
          businessName: "Fresh Market",
          category: "Grocery",
          city: "Berlin",
          primaryImageUrl: null,
          status: "Active",
          pointsBalance: 240,
          lifetimePoints: 640,
          lastAccrualAtUtc: "2026-04-24T09:00:00Z",
        },
      ],
      loyaltyBusinessesStatus: "ok",
      storefrontCart,
    }),
  );

  assert.match(
    html,
    /linear-gradient\(135deg,#f5ffe8_0%,#ffffff_42%,#fff1d0_100%\)/,
  );
  assert.match(html, /Action center/);
  assert.match(html, /Orders and invoices/);
  assert.doesNotMatch(html, /Storefront continuation/);
  assert.doesNotMatch(html, /Promotion lanes/);
  assert.ok(html.includes('href="/en-US/checkout"') || html.includes('href="/checkout"'));
  assert.ok(
    html.includes('href="/en-US/orders/order-1"') ||
      html.includes('href="/orders/order-1"'),
  );
});
