import assert from "node:assert/strict";
import test from "node:test";
import { getCommerceResource, resolveLocalizedQueryMessage } from "@/localization";

test("resolveLocalizedQueryMessage falls back to shared resources for bundle-external keys", () => {
  const commerce = getCommerceResource("de-DE");

  assert.equal(
    resolveLocalizedQueryMessage("i18n:publicApiNetworkErrorMessage", commerce),
    "Die oeffentliche API konnte nicht erreicht werden.",
  );
});
