import assert from "node:assert/strict";
import test from "node:test";
import { getCmsIndexSeoMetadata } from "@/features/cms/server/get-cms-index-seo-metadata";

test("getCmsIndexSeoMetadata uses the customer-facing help route as the canonical index", async () => {
  const result = await getCmsIndexSeoMetadata("en-US");

  assert.equal(result.canonicalPath, "/help");
  assert.equal(result.noIndex, false);
  assert.deepEqual(result.languageAlternates, {
    "x-default": "/help",
    "de-DE": "/help",
    "en-US": "/help",
  });
  assert.equal(result.metadata.alternates?.canonical, "/help");
});

test("getCmsIndexSeoMetadata keeps filtered help listing pages out of the index", async () => {
  const result = await getCmsIndexSeoMetadata("en-US", 2, "returns");

  assert.equal(result.canonicalPath, "/help?page=2&search=returns");
  assert.equal(result.noIndex, true);
  assert.equal(result.languageAlternates, undefined);
  assert.deepEqual(result.metadata.robots, {
    index: false,
    follow: false,
    googleBot: {
      index: false,
      follow: false,
    },
  });
});
