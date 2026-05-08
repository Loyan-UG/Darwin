import assert from "node:assert/strict";
import test from "node:test";
import {
  getCommerceResource,
  getMemberResource,
  getSharedResource,
  resolveApiStatusLabel,
  resolveLocalizedQueryMessage,
  resolveProblemQueryMessage,
  resolveStatusMappedMessage,
} from "@/localization";

test("resolveLocalizedQueryMessage falls back to shared resources for bundle-external keys", () => {
  const commerce = getCommerceResource("de-DE");

  assert.equal(
    resolveLocalizedQueryMessage("i18n:publicApiNetworkErrorMessage", commerce),
    "Einige Inhalte sind voruebergehend nicht verfuegbar.",
  );
});

test("resolveProblemQueryMessage keeps only localized problem details", () => {
  assert.equal(
    resolveProblemQueryMessage(
      {
        detail: "i18n:memberApiHttpErrorMessage",
        title: "Ignored fallback",
      },
      "publicApiHttpErrorMessage",
    ),
    "i18n:memberApiHttpErrorMessage",
  );

  assert.equal(
    resolveProblemQueryMessage(
      {
        detail: "Plain backend detail",
        title: "Plain backend title",
      },
      "publicApiHttpErrorMessage",
    ),
    "i18n:publicApiHttpErrorMessage",
  );
});

test("resolveApiStatusLabel localizes shared API status tokens", () => {
  const shared = getSharedResource("de-DE");

  assert.equal(
    resolveApiStatusLabel("network-error", shared),
    "Voruebergehend nicht verfuegbar",
  );
  assert.equal(resolveApiStatusLabel("not-found", shared), "Nicht verfuegbar");
  assert.equal(resolveApiStatusLabel("custom-status", shared), "custom-status");
});

test("resolveStatusMappedMessage resolves mapped status keys across bundle registries", () => {
  const member = getMemberResource("en-US");

  assert.equal(
    resolveStatusMappedMessage("unauthorized", member, {
      unauthorized: "memberSessionUnauthorizedMessage",
    }),
    "Please sign in again to continue.",
  );

  assert.equal(
    resolveStatusMappedMessage("network-error", member, {
      "network-error": "publicApiNetworkErrorMessage",
    }),
    "Some content is temporarily unavailable.",
  );
});
