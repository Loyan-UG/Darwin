import assert from "node:assert/strict";
import test from "node:test";
import {
  buildLocalizedDiscoveryLoaderDiagnostics,
  buildLocalizedDiscoveryLoaderObservationContext,
  buildLocalizedDiscoveryLoaderObservationSuccessContext,
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

  assert.deepEqual(buildLocalizedDiscoveryLoaderDiagnostics("inventory"), {
    localizedDiscoveryKind: "inventory",
    localizedDiscoveryNormalization: "canonical-cultures",
  });

  assert.deepEqual(buildLocalizedDiscoveryLoaderDiagnostics("sitemap"), {
    localizedDiscoveryKind: "sitemap",
    localizedDiscoveryNormalization: "canonical-cultures",
  });
});

test("localized-discovery loader helper builders keep canonical observation and success diagnostics explicit", () => {
  assert.deepEqual(
    buildLocalizedDiscoveryLoaderObservationContext("inventory", {
      supportedCultures: "de-DE|en-US",
    }),
    {
      localizedDiscoveryKind: "inventory",
      localizedDiscoveryNormalization: "canonical-cultures",
      supportedCultures: "de-DE|en-US",
    },
  );

  assert.deepEqual(
    buildLocalizedDiscoveryLoaderObservationSuccessContext("sitemap", {
      totalEntryCount: 8,
      sitemapSummaryFootprint: "total:8|static:6|cms:1|products:1",
      sitemapCompositionFootprint: "static:6|cms:1|products:1",
    }),
    {
      localizedDiscoveryKind: "sitemap",
      localizedDiscoveryNormalization: "canonical-cultures",
      localizedDiscoveryState: "present",
      localizedDiscoveryDetailFootprint: "static:6|cms:1|products:1",
      localizedDiscoverySummaryFootprint: "total:8|static:6|cms:1|products:1",
      totalEntryCount: 8,
      sitemapSummaryFootprint: "total:8|static:6|cms:1|products:1",
      sitemapCompositionFootprint: "static:6|cms:1|products:1",
    },
  );

  assert.deepEqual(
    buildLocalizedDiscoveryLoaderObservationContext("sitemap"),
    {
      localizedDiscoveryKind: "sitemap",
      localizedDiscoveryNormalization: "canonical-cultures",
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
test("createLocalizedDiscoveryLoader keeps base diagnostics explicit when context hooks return undefined", async () => {
  const loader = createLocalizedDiscoveryLoader({
    kind: "inventory",
    area: "unit-localized-discovery",
    operation: "load-undefined-context",
    getContext: () => undefined,
    getSuccessContext: () => undefined,
    load: async () => ({
      pageCount: 0,
    }),
  });

  const result = await loader();

  assert.deepEqual(result, {
    pageCount: 0,
  });
});
test("createLocalizedDiscoveryLoader keeps empty observation context branches explicit", async () => {
  const loader = createLocalizedDiscoveryLoader({
    kind: "sitemap",
    area: "unit-localized-discovery",
    operation: "load-empty-context",
    getContext: () => ({}),
    getSuccessContext: (result) => ({ totalEntryCount: result.totalEntryCount }),
    load: async () => ({
      totalEntryCount: 0,
    }),
  });

  const result = await loader();

  assert.deepEqual(result, {
    totalEntryCount: 0,
  });
});

test("createLocalizedDiscoveryLoader keeps undefined success-context branches explicit", async () => {
  const loader = createLocalizedDiscoveryLoader({
    kind: "inventory",
    area: "unit-localized-discovery",
    operation: "load-undefined-success-context",
    getContext: () => ({ supportedCultures: 2 }),
    getSuccessContext: () => undefined,
    load: async () => ({
      pageCount: 0,
    }),
  });

  const result = await loader();

  assert.deepEqual(result, {
    pageCount: 0,
  });
});test("createLocalizedDiscoveryLoader keeps empty success-context branches explicit", async () => {
  const loader = createLocalizedDiscoveryLoader({
    kind: "inventory",
    area: "unit-localized-discovery",
    operation: "load-empty-success-context",
    getContext: () => ({ supportedCultures: 2 }),
    getSuccessContext: () => ({}),
    load: async () => ({
      pageCount: 0,
    }),
  });

  const result = await loader();

  assert.deepEqual(result, {
    pageCount: 0,
  });
});


