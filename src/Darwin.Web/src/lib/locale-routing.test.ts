import test from "node:test";
import assert from "node:assert/strict";
import {
  appendAppQueryParam,
  buildAppQueryPath,
  buildLocalizedAuthHref,
  buildLocalizedPath,
  buildLocalizedQueryHref,
  localizeHref,
  sanitizeAppPath,
  stripCulturePrefix,
} from "@/lib/locale-routing";

test("sanitizeAppPath keeps only app-local paths and preserves safe query/hash", () => {
  assert.equal(
    sanitizeAppPath("/checkout?step=payment#summary"),
    "/checkout?step=payment#summary",
  );
  assert.equal(sanitizeAppPath("https://evil.example.com"), "/");
  assert.equal(sanitizeAppPath("//evil.example.com"), "/");
  assert.equal(sanitizeAppPath("catalog"), "/");
  assert.equal(sanitizeAppPath(undefined, "/account"), "/account");
});

test("localized helpers keep public routes canonical across supported cultures", () => {
  const previousCulture = process.env.DARWIN_WEB_CULTURE;
  const previousSupportedCultures = process.env.DARWIN_WEB_SUPPORTED_CULTURES;
  process.env.DARWIN_WEB_CULTURE = "de-DE";
  process.env.DARWIN_WEB_SUPPORTED_CULTURES = "de-DE,en-US";

  try {
    assert.equal(buildLocalizedPath("/catalog", "de-DE"), "/catalog");
    assert.equal(buildLocalizedPath("/catalog", "en-US"), "/catalog");
    assert.equal(localizeHref("/catalog?page=2", "en-US"), "/catalog?page=2");
    assert.equal(
      buildLocalizedQueryHref("/catalog", { category: "coffee", page: 2 }, "en-US"),
      "/catalog?category=coffee&page=2",
    );
    assert.equal(
      buildLocalizedAuthHref("/account/sign-in", "https://evil.example.com", "en-US"),
      "/account/sign-in?returnPath=%2Faccount",
    );
    assert.equal(stripCulturePrefix("/en-US/page/impressum").pathname, "/page/impressum");
  } finally {
    if (previousCulture === undefined) {
      delete process.env.DARWIN_WEB_CULTURE;
    } else {
      process.env.DARWIN_WEB_CULTURE = previousCulture;
    }

    if (previousSupportedCultures === undefined) {
      delete process.env.DARWIN_WEB_SUPPORTED_CULTURES;
    } else {
      process.env.DARWIN_WEB_SUPPORTED_CULTURES = previousSupportedCultures;
    }
  }
});

test("app query helpers build stable internal query paths", () => {
  assert.equal(
    buildAppQueryPath("/catalog", { category: "tea", page: 3, visibleQuery: "" }),
    "/catalog?category=tea&page=3",
  );
  assert.equal(
    appendAppQueryParam("/checkout?step=shipping", "paymentError", "declined"),
    "/checkout?step=shipping&paymentError=declined",
  );
  assert.equal(appendAppQueryParam("/checkout", "paymentError", ""), "/checkout");
});
