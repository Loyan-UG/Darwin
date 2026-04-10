import assert from "node:assert/strict";
import test from "node:test";
import {
  buildLocalizedDiscoveryLoaderDiagnostics,
  createLocalizedDiscoveryLoader,
} from "@/lib/localized-discovery-loader";
import { getLocalizedDiscoveryNormalizationMode } from "@/lib/localized-discovery-loader-diagnostics";

test("getLocalizedDiscoveryNormalizationMode keeps canonical versus raw explicit", () => {
  assert.equal(
    getLocalizedDiscoveryNormalizationMode(true),
    "canonical-cultures",
  );
  assert.equal(
    getLocalizedDiscoveryNormalizationMode(false),
    "raw-cultures",
  );
  assert.equal(
    getLocalizedDiscoveryNormalizationMode(undefined),
    "raw-cultures",
  );
});

test("buildLocalizedDiscoveryLoaderDiagnostics keeps localized discovery kind explicit", () => {
  assert.deepEqual(
    buildLocalizedDiscoveryLoaderDiagnostics("inventory", {
      cultureCount: 2,
    }),
    {
      localizedDiscoveryKind: "inventory",
      localizedDiscoveryNormalization: "canonical-cultures",
      cultureCount: 2,
    },
  );
});

test("createLocalizedDiscoveryLoader forwards inventory results through the shared loader wrapper", async () => {
  let executions = 0;
  const loader = createLocalizedDiscoveryLoader({
    kind: "inventory",
    area: "unit-localized-discovery",
    operation: "load-inventory",
    getContext: () => ({ supportedCultures: 2 }),
    getSuccessContext: (result) => ({ pageCount: result.pageCount }),
    load: async () => {
      executions += 1;
      return {
        pageCount: executions,
      };
    },
  });

  const first = await loader();

  assert.equal(executions, 1);
  assert.equal(first.pageCount, 1);
});

test("createLocalizedDiscoveryLoader feeds canonical context and success diagnostics through the wrapper", async () => {
  let contextSnapshot: Record<string, unknown> | undefined;
  let successSnapshot: Record<string, unknown> | undefined;

  const loader = createLocalizedDiscoveryLoader({
    kind: "sitemap",
    area: "unit-localized-discovery",
    operation: "load-sitemap",
    getContext: () => {
      contextSnapshot = {
        supportedCultures: "de-DE|en-US",
      };
      return contextSnapshot;
    },
    getSuccessContext: (result) => {
      successSnapshot = {
        totalEntryCount: result.totalEntryCount,
        sitemapSummaryFootprint: result.sitemapSummaryFootprint,
      };
      return successSnapshot;
    },
    load: async () => ({
      totalEntryCount: 8,
      sitemapSummaryFootprint: "total:8|static:6|cms:1|products:1",
    }),
  });

  const result = await loader();

  assert.equal(result.totalEntryCount, 8);
  assert.deepEqual(contextSnapshot, {
    supportedCultures: "de-DE|en-US",
  });
  assert.deepEqual(successSnapshot, {
    totalEntryCount: 8,
    sitemapSummaryFootprint: "total:8|static:6|cms:1|products:1",
  });
});
