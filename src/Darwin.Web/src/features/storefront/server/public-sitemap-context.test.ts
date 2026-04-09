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
        path: "/cms/impressum",
        languageAlternates: {
          "de-DE": "/cms/impressum",
          "en-US": "/en-US/cms/imprint",
        },
      },
    ],
    productSitemapEntries: [
      {
        path: "/catalog/kaffee",
        languageAlternates: {
          "de-DE": "/catalog/kaffee",
          "en-US": "/en-US/catalog/coffee",
        },
      },
    ],
  });

  assert.equal(result.staticEntryCount, 6);
  assert.equal(result.cmsEntryCount, 1);
  assert.equal(result.productEntryCount, 1);
  assert.equal(result.entries.length, 8);
  assert.equal(result.entries[0]?.url, `${siteUrl}/`);
  assert.deepEqual(result.entries[6]?.alternates?.languages, {
    "de-DE": `${siteUrl}/cms/impressum`,
    "en-US": `${siteUrl}/en-US/cms/imprint`,
  });
  assert.deepEqual(result.entries[7]?.alternates?.languages, {
    "de-DE": `${siteUrl}/catalog/kaffee`,
    "en-US": `${siteUrl}/en-US/catalog/coffee`,
  });
});

test("buildPublicSitemapEntries canonicalizes alternate ordering before emitting sitemap languages", () => {
  const { siteUrl } = getSiteRuntimeConfig();
  const result = buildPublicSitemapEntries({
    supportedCultures: ["de-DE", "en-US"],
    cmsSitemapEntries: [
      {
        path: "/cms/impressum",
        languageAlternates: {
          "en-US": "/en-US/cms/imprint",
          "x-default": "/cms/impressum",
          "de-DE": "/cms/impressum",
        },
      },
    ],
    productSitemapEntries: [],
  });

  assert.deepEqual(result.entries[6]?.alternates?.languages, {
    "x-default": `${siteUrl}/cms/impressum`,
    "de-DE": `${siteUrl}/cms/impressum`,
    "en-US": `${siteUrl}/en-US/cms/imprint`,
  });
});

test("buildPublicSitemapEntries canonicalizes supported cultures before emitting static entries", () => {
  const { siteUrl } = getSiteRuntimeConfig();
  const result = buildPublicSitemapEntries({
    supportedCultures: [" en-US ", "de-DE", "en-US", "fr-FR"],
    cmsSitemapEntries: [],
    productSitemapEntries: [],
  });

  assert.equal(result.staticEntryCount, 6);
  assert.deepEqual(
    result.entries.slice(0, 6).map((entry) => entry.url),
    [
      `${siteUrl}/`,
      `${siteUrl}/en-US`,
      `${siteUrl}/catalog`,
      `${siteUrl}/en-US/catalog`,
      `${siteUrl}/cms`,
      `${siteUrl}/en-US/cms`,
    ],
  );
});
