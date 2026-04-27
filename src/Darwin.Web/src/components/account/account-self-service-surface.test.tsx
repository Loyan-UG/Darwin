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
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import type {
  MemberAddress,
  MemberCustomerProfile,
  MemberPreferences,
} from "@/features/member-portal/types";
import type { MemberSession } from "@/features/member-session/types";
import { getMemberResource } from "@/localization";

const stubDirectory = fs.mkdtempSync(path.join(os.tmpdir(), "darwin-account-self-service-"));
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

const copy = getMemberResource("en-US");

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

const profile: MemberCustomerProfile = {
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
};

const preferences: MemberPreferences = {
  marketingConsent: true,
  allowEmailMarketing: true,
  allowSmsMarketing: true,
  allowWhatsAppMarketing: false,
  allowPromotionalPushNotifications: false,
  allowOptionalAnalyticsTracking: true,
  rowVersion: "pref-rv-1",
};

const addresses: MemberAddress[] = [
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
];

const session: MemberSession = {
  email: "ada@example.com",
  isAuthenticated: true,
  accessTokenExpiresAtUtc: "2026-04-27T12:00:00Z",
};

const gradientPattern = /f5ffe8|fff1d0/;

test("ProfilePage renders the upgraded grocery self-service surface", async () => {
  const { ProfilePage } = await import("@/components/account/profile-page");
  const html = renderToStaticMarkup(
    React.createElement(ProfilePage, {
      culture: "en-US",
      profile,
      supportedCultures: ["en-US", "de-DE"],
      status: "ok",
      profileStatus: "saved",
      phoneStatus: "confirmed",
      cmsPages: [cmsPage],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [product],
      productsStatus: "ok",
    }),
  );

  assert.match(html, gradientPattern);
  assert.match(html, new RegExp(copy.profileEditTitle));
  assert.match(html, new RegExp(copy.profileReadinessTitle));
  assert.match(html, new RegExp(copy.phoneVerificationTitle));
  assert.ok(
    html.includes('href="/en-US/account/preferences"') ||
      html.includes('href="/account/preferences"'),
  );
});

test("PreferencesPage renders the upgraded grocery self-service surface", async () => {
  const { PreferencesPage } = await import("@/components/account/preferences-page");
  const html = renderToStaticMarkup(
    React.createElement(PreferencesPage, {
      culture: "en-US",
      preferences,
      status: "ok",
      profile,
      profileStatus: "ok",
      preferencesStatus: "saved",
      cmsPages: [cmsPage],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [product],
      productsStatus: "ok",
    }),
  );

  assert.match(html, gradientPattern);
  assert.match(html, new RegExp(copy.preferencesEditTitle));
  assert.match(html, new RegExp(copy.preferencesChannelReadinessTitle));
  assert.match(html, new RegExp(copy.preferencesRouteSummaryTitle));
  assert.ok(
    html.includes('href="/en-US/account/addresses"') ||
      html.includes('href="/account/addresses"'),
  );
});

test("AddressesPage renders the upgraded grocery self-service surface", async () => {
  const { AddressesPage } = await import("@/components/account/addresses-page");
  const html = renderToStaticMarkup(
    React.createElement(AddressesPage, {
      culture: "en-US",
      addresses,
      status: "ok",
      addressesStatus: "updated",
      cmsPages: [cmsPage],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [product],
      productsStatus: "ok",
    }),
  );

  assert.match(html, gradientPattern);
  assert.match(html, new RegExp(copy.addressesTitle));
  assert.match(html, new RegExp(copy.addressesReadinessTitle));
  assert.match(html, new RegExp(copy.savedAddressLabel));
  assert.ok(
    html.includes('href="/en-US/checkout') || html.includes('href="/checkout'),
  );
});

test("SecurityPage renders the upgraded grocery self-service surface", async () => {
  const { SecurityPage } = await import("@/components/account/security-page");
  const html = renderToStaticMarkup(
    React.createElement(SecurityPage, {
      culture: "en-US",
      session,
      profile,
      profileStatus: "ok",
      securityStatus: "saved",
      cmsPages: [cmsPage],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [product],
      productsStatus: "ok",
    }),
  );

  assert.match(html, gradientPattern);
  assert.match(html, new RegExp(copy.securityEditTitle));
  assert.match(html, new RegExp(copy.securityCurrentStateTitle));
  assert.match(html, new RegExp(copy.memberRouteSummaryTitle));
  assert.ok(
    html.includes('href="/en-US/account/profile"') ||
      html.includes('href="/account/profile"'),
  );
});
