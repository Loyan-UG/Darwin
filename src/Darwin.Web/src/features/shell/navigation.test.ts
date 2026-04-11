import test from "node:test";
import assert from "node:assert/strict";
import {
  getFallbackFooterGroups,
  getFallbackPrimaryNavigation,
  getUtilityLinks,
} from "@/features/shell/navigation";

test("fallback shell navigation keeps primary storefront routes discoverable", () => {
  assert.deepEqual(
    getFallbackPrimaryNavigation("en-US").map((link) => link.href),
    ["/", "/catalog", "/cms", "/account", "/cart", "/checkout", "/orders", "/invoices"],
  );

  assert.deepEqual(
    getFallbackPrimaryNavigation("de-DE").map((link) => link.href),
    ["/", "/catalog", "/cms", "/account", "/cart", "/checkout", "/orders", "/invoices"],
  );
});

test("fallback shell footer keeps member and mock-checkout routes reachable", () => {
  const englishFooterLinks = getFallbackFooterGroups("en-US")
    .flatMap((group) => group.links)
    .map((link) => link.href);
  const germanFooterLinks = getFallbackFooterGroups("de-DE")
    .flatMap((group) => group.links)
    .map((link) => link.href);

  for (const links of [englishFooterLinks, germanFooterLinks]) {
    assert.ok(links.includes("/cms/impressum"));
    assert.ok(links.includes("/cms/kontakt"));
    assert.ok(links.includes("/account/sign-in"));
    assert.ok(links.includes("/account/register"));
    assert.ok(links.includes("/account/profile"));
    assert.ok(links.includes("/account/preferences"));
    assert.ok(links.includes("/account/addresses"));
    assert.ok(links.includes("/account/security"));
    assert.ok(links.includes("/mock-checkout"));
  }
});

test("shell utility links keep live commerce entry points reachable", () => {
  assert.deepEqual(
    getUtilityLinks("en-US").map((link) => link.href),
    ["/catalog", "/cart", "/checkout"],
  );
  assert.deepEqual(
    getUtilityLinks("de-DE").map((link) => link.href),
    ["/catalog", "/cart", "/checkout"],
  );
});
