import assert from "node:assert/strict";
import test from "node:test";
import { buildPublicSitemapEntries } from "@/features/storefront/server/localized-public-discovery-projections";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

test("buildPublicSitemapEntries combines static and localized detail entries into one sitemap payload", () => {
  const { siteUrl } = getSiteRuntimeConfig();
  const result = buildPublicSitemapEntries({
    supportedCultures: ["de-DE", "en-US"],
    cmsSitemapEntries: [
      {
        path: "/page/impressum",
        languageAlternates: {
          "de-DE": "/page/impressum",
          "en-US": "/page/imprint",
        },
      },
    ],
    productSitemapEntries: [
      {
        path: "/catalog/kaffee",
        languageAlternates: {
          "de-DE": "/catalog/kaffee",
          "en-US": "/catalog/coffee",
        },
      },
    ],
  });

  assert.equal(result.staticEntryCount, 3);
  assert.equal(result.cmsEntryCount, 1);
  assert.equal(result.productEntryCount, 1);
  assert.equal(result.entries.length, 5);
  assert.equal(result.entries[0]?.url, `${siteUrl}/`);
  assert.deepEqual(result.entries[3]?.alternates?.languages, {
    "de-DE": `${siteUrl}/page/impressum`,
    "en-US": `${siteUrl}/page/imprint`,
  });
  assert.deepEqual(result.entries[4]?.alternates?.languages, {
    "de-DE": `${siteUrl}/catalog/kaffee`,
    "en-US": `${siteUrl}/catalog/coffee`,
  });
});

test("buildPublicSitemapEntries canonicalizes alternate ordering before emitting sitemap languages", () => {
  const { siteUrl } = getSiteRuntimeConfig();
  const result = buildPublicSitemapEntries({
    supportedCultures: ["de-DE", "en-US"],
    cmsSitemapEntries: [
      {
        path: "/page/impressum",
        languageAlternates: {
          "en-US": "/page/imprint",
          "x-default": "/page/impressum",
          "de-DE": "/page/impressum",
        },
      },
    ],
    productSitemapEntries: [],
  });

  assert.deepEqual(result.entries[3]?.alternates?.languages, {
    "x-default": `${siteUrl}/page/impressum`,
    "de-DE": `${siteUrl}/page/impressum`,
    "en-US": `${siteUrl}/page/imprint`,
  });
});

test("buildPublicSitemapEntries canonicalizes supported cultures before emitting static entries", () => {
  const { siteUrl } = getSiteRuntimeConfig();
  const result = buildPublicSitemapEntries({
    supportedCultures: [" en-US ", "de-DE", "en-US", "fr-FR"],
    cmsSitemapEntries: [],
    productSitemapEntries: [],
  });

  assert.equal(result.staticEntryCount, 3);
  assert.deepEqual(
    result.entries.slice(0, 3).map((entry) => entry.url),
    [
      `${siteUrl}/`,
      `${siteUrl}/catalog`,
      `${siteUrl}/help`,
    ],
  );
});
