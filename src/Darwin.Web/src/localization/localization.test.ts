import assert from "node:assert/strict";
import test from "node:test";
import { getCommerceResource, resolveLocalizedQueryMessage } from "@/localization";

test("resolveLocalizedQueryMessage falls back to shared resources for bundle-external keys", () => {
  const commerce = getCommerceResource("de-DE");

  assert.equal(
    resolveLocalizedQueryMessage("i18n:publicApiNetworkErrorMessage", commerce),
    "Ein Teil der Storefront-Inhalte ist momentan nicht verfuegbar.",
  );
});
