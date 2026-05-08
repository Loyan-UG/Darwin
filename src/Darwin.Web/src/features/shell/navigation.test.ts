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
    ["/", "/catalog", "/loyalty", "/help", "/page/contact"],
  );

  assert.deepEqual(
    getFallbackPrimaryNavigation("de-DE").map((link) => link.href),
    ["/", "/catalog", "/loyalty", "/help", "/page/kontakt"],
  );
});

test("fallback shell footer keeps customer-facing shop, account, support, and legal routes reachable", () => {
  const englishFooterLinks = getFallbackFooterGroups("en-US")
    .flatMap((group) => group.links)
    .map((link) => link.href);
  const germanFooterLinks = getFallbackFooterGroups("de-DE")
    .flatMap((group) => group.links)
    .map((link) => link.href);

  for (const links of [englishFooterLinks, germanFooterLinks]) {
    assert.ok(links.includes("/catalog"));
    assert.ok(links.includes("/loyalty"));
    assert.ok(links.includes("/cart"));
    assert.ok(links.includes("/account/sign-in"));
    assert.ok(links.includes("/account/register"));
    assert.ok(links.includes("/account"));
    assert.ok(links.includes("/orders"));
    assert.ok(links.includes("/invoices"));
    assert.ok(links.includes("/help"));
    assert.ok(
      links.includes("/page/contact") || links.includes("/page/kontakt"),
    );
    assert.ok(
      links.includes("/page/legal-notice") || links.includes("/page/impressum"),
    );
    assert.equal(links.includes("/mock-checkout"), false);
  }
});

test("shell utility links keep account and cart entry points reachable", () => {
  assert.deepEqual(
    getUtilityLinks("en-US").map((link) => link.href),
    ["/account", "/cart"],
  );
  assert.deepEqual(
    getUtilityLinks("de-DE").map((link) => link.href),
    ["/account", "/cart"],
  );
});
