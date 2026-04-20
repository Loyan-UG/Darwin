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
    "Ein Teil der Storefront-Inhalte ist momentan nicht verfuegbar.",
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

  assert.equal(resolveApiStatusLabel("network-error", shared), "Netzwerkproblem");
  assert.equal(resolveApiStatusLabel("not-found", shared), "nicht gefunden");
  assert.equal(resolveApiStatusLabel("custom-status", shared), "custom-status");
});

test("resolveStatusMappedMessage resolves mapped status keys across bundle registries", () => {
  const member = getMemberResource("en-US");

  assert.equal(
    resolveStatusMappedMessage("unauthorized", member, {
      unauthorized: "memberSessionUnauthorizedMessage",
    }),
    "The current member session cannot access this route.",
  );

  assert.equal(
    resolveStatusMappedMessage("network-error", member, {
      "network-error": "publicApiNetworkErrorMessage",
    }),
    "Part of the storefront content is temporarily unavailable.",
  );
});
