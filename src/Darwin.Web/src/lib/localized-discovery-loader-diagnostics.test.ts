import assert from "node:assert/strict";
import test from "node:test";
import {
  buildLocalizedDiscoveryDetailFootprint,
  buildLocalizedDiscoveryLoaderBaseDiagnostics,
  buildLocalizedDiscoveryLoaderSuccessDiagnostics,
  buildLocalizedDiscoveryState,
  buildLocalizedDiscoverySummaryFootprint,
} from "@/lib/localized-discovery-loader-diagnostics";

test("buildLocalizedDiscoveryState prefers explicit state and otherwise derives from numeric signals", () => {
  assert.equal(
    buildLocalizedDiscoveryState({
      localizedDiscoveryState: "present",
      localizedCultureCount: 0,
    }),
    "present",
  );

  assert.equal(
    buildLocalizedDiscoveryState({
      localizedCultureCount: 2,
      localizedItemCount: 0,
    }),
    "present",
  );

  assert.equal(
    buildLocalizedDiscoveryState({
      localizedCultureCount: 0,
      localizedItemCount: 0,
      totalEntryCount: 0,
    }),
    "empty",
  );

  assert.equal(buildLocalizedDiscoveryState({}), "unknown");
});

test("buildLocalizedDiscoverySummaryFootprint prefers canonical summary fields", () => {
  assert.equal(
    buildLocalizedDiscoverySummaryFootprint({
      localizedDiscoverySummaryFootprint: "cultures:2|items:4|empty:0",
      localizedInventorySummaryFootprint: "cultures:9|items:9|empty:9",
    }),
    "cultures:2|items:4|empty:0",
  );

  assert.equal(
    buildLocalizedDiscoverySummaryFootprint({
      sitemapSummaryFootprint: "total:8|static:6|cms:1|products:1",
    }),
    "total:8|static:6|cms:1|products:1",
  );

  assert.equal(buildLocalizedDiscoverySummaryFootprint({}), "summary:none");
});

test("buildLocalizedDiscoveryDetailFootprint prefers canonical detail fields", () => {
  assert.equal(
    buildLocalizedDiscoveryDetailFootprint({
      localizedDiscoveryDetailFootprint: "static:5|cms:2|products:1",
      sitemapCompositionFootprint: "static:6|cms:1|products:1",
    }),
    "static:5|cms:2|products:1",
  );

  assert.equal(
    buildLocalizedDiscoveryDetailFootprint({
      alternateMapFootprint: "items:2|alternates:3|multi:1",
    }),
    "items:2|alternates:3|multi:1",
  );

  assert.equal(buildLocalizedDiscoveryDetailFootprint({}), "detail:none");
});

test("buildLocalizedDiscoveryLoaderBaseDiagnostics keeps kind and normalization explicit", () => {
  assert.deepEqual(
    buildLocalizedDiscoveryLoaderBaseDiagnostics("inventory", {
      hasCanonicalCultureNormalization: true,
      extras: {
        scope: "localized-discovery-inventory",
        cultureCount: 2,
      },
    }),
    {
      localizedDiscoveryKind: "inventory",
      localizedDiscoveryNormalization: "canonical-cultures",
      scope: "localized-discovery-inventory",
      cultureCount: 2,
    },
  );
});

test("buildLocalizedDiscoveryLoaderSuccessDiagnostics preserves success detail with canonical mode", () => {
  assert.deepEqual(
    buildLocalizedDiscoveryLoaderSuccessDiagnostics(
      "sitemap",
      {
        totalEntryCount: 8,
        cmsEntryCount: 2,
        sitemapSummaryFootprint: "total:8|static:6|cms:1|products:1",
        sitemapCompositionFootprint: "static:6|cms:1|products:1",
      },
      {
        hasCanonicalCultureNormalization: true,
      },
    ),
    {
      localizedDiscoveryKind: "sitemap",
      localizedDiscoveryNormalization: "canonical-cultures",
      localizedDiscoveryState: "present",
      localizedDiscoveryDetailFootprint: "static:6|cms:1|products:1",
      localizedDiscoverySummaryFootprint: "total:8|static:6|cms:1|products:1",
      totalEntryCount: 8,
      cmsEntryCount: 2,
      sitemapSummaryFootprint: "total:8|static:6|cms:1|products:1",
      sitemapCompositionFootprint: "static:6|cms:1|products:1",
    },
  );
});

test("buildLocalizedDiscoveryLoaderSuccessDiagnostics prefers standardized localized summary footprints", () => {
  assert.deepEqual(
    buildLocalizedDiscoveryLoaderSuccessDiagnostics(
      "inventory",
      {
        localizedCultureCount: 2,
        localizedInventorySummaryFootprint: "cultures:2|items:4|empty:0",
        localizedInventoryFootprint: "cultures:2|items:4|empty:0",
      },
      {
        hasCanonicalCultureNormalization: true,
      },
    ),
    {
      localizedDiscoveryKind: "inventory",
      localizedDiscoveryNormalization: "canonical-cultures",
      localizedDiscoveryState: "present",
      localizedDiscoveryDetailFootprint: "cultures:2|items:4|empty:0",
      localizedDiscoverySummaryFootprint: "cultures:2|items:4|empty:0",
      localizedCultureCount: 2,
      localizedInventorySummaryFootprint: "cultures:2|items:4|empty:0",
      localizedInventoryFootprint: "cultures:2|items:4|empty:0",
    },
  );
});

test("buildLocalizedDiscoveryLoaderSuccessDiagnostics prefers canonical detail footprints over branch-local ones", () => {
  assert.deepEqual(
    buildLocalizedDiscoveryLoaderSuccessDiagnostics(
      "sitemap",
      {
        localizedDiscoveryState: "present",
        localizedDiscoveryDetailFootprint: "static:5|cms:2|products:1",
        localizedDiscoverySummaryFootprint:
          "total:8|static:5|cms:2|products:1",
        sitemapCompositionFootprint: "static:6|cms:1|products:1",
        sitemapSummaryFootprint: "total:8|static:6|cms:1|products:1",
      },
      {
        hasCanonicalCultureNormalization: true,
      },
    ),
    {
      localizedDiscoveryKind: "sitemap",
      localizedDiscoveryNormalization: "canonical-cultures",
      localizedDiscoveryState: "present",
      localizedDiscoveryDetailFootprint: "static:5|cms:2|products:1",
      localizedDiscoverySummaryFootprint:
        "total:8|static:5|cms:2|products:1",
      sitemapCompositionFootprint: "static:6|cms:1|products:1",
      sitemapSummaryFootprint: "total:8|static:6|cms:1|products:1",
    },
  );
});

test("buildLocalizedDiscoveryLoaderSuccessDiagnostics falls back to an explicit empty summary footprint", () => {
  assert.deepEqual(
    buildLocalizedDiscoveryLoaderSuccessDiagnostics(
      "inventory",
      {
        localizedCultureCount: 0,
      },
      {
        hasCanonicalCultureNormalization: true,
      },
    ),
    {
      localizedDiscoveryKind: "inventory",
      localizedDiscoveryNormalization: "canonical-cultures",
      localizedDiscoveryState: "empty",
      localizedDiscoveryDetailFootprint: "detail:none",
      localizedDiscoverySummaryFootprint: "summary:none",
      localizedCultureCount: 0,
    },
  );
});

test("buildLocalizedDiscoveryLoaderSuccessDiagnostics falls back to an explicit empty detail footprint", () => {
  assert.deepEqual(
    buildLocalizedDiscoveryLoaderSuccessDiagnostics(
      "sitemap",
      {},
      {
        hasCanonicalCultureNormalization: true,
      },
    ),
    {
      localizedDiscoveryKind: "sitemap",
      localizedDiscoveryNormalization: "canonical-cultures",
      localizedDiscoveryState: "unknown",
      localizedDiscoveryDetailFootprint: "detail:none",
      localizedDiscoverySummaryFootprint: "summary:none",
    },
  );
});

test("buildLocalizedDiscoveryLoaderSuccessDiagnostics keeps raw-culture mode explicit", () => {
  assert.deepEqual(
    buildLocalizedDiscoveryLoaderSuccessDiagnostics(
      "inventory",
      {
        localizedCultureCount: 1,
        localizedItemCount: 3,
        localizedInventoryFootprint: "cultures:1|items:3|empty:0",
      },
      {
        hasCanonicalCultureNormalization: false,
      },
    ),
    {
      localizedDiscoveryKind: "inventory",
      localizedDiscoveryNormalization: "raw-cultures",
      localizedDiscoveryState: "present",
      localizedDiscoveryDetailFootprint: "cultures:1|items:3|empty:0",
      localizedDiscoverySummaryFootprint: "cultures:1|items:3|empty:0",
      localizedCultureCount: 1,
      localizedItemCount: 3,
      localizedInventoryFootprint: "cultures:1|items:3|empty:0",
    },
  );
});

test("buildLocalizedDiscoveryLoaderBaseDiagnostics keeps raw-culture mode explicit", () => {
  assert.deepEqual(
    buildLocalizedDiscoveryLoaderBaseDiagnostics("sitemap", {
      hasCanonicalCultureNormalization: false,
      extras: {
        scope: "localized-discovery-sitemap",
        totalEntryCount: 5,
      },
    }),
    {
      localizedDiscoveryKind: "sitemap",
      localizedDiscoveryNormalization: "raw-cultures",
      scope: "localized-discovery-sitemap",
      totalEntryCount: 5,
    },
  );

  assert.deepEqual(buildLocalizedDiscoveryLoaderBaseDiagnostics("inventory"), {
    localizedDiscoveryKind: "inventory",
    localizedDiscoveryNormalization: "raw-cultures",
  });

  assert.deepEqual(buildLocalizedDiscoveryLoaderBaseDiagnostics("sitemap"), {
    localizedDiscoveryKind: "sitemap",
    localizedDiscoveryNormalization: "raw-cultures",
  });
});

test("buildLocalizedDiscoveryLoaderSuccessDiagnostics keeps raw sitemap mode explicit", () => {
  assert.deepEqual(
    buildLocalizedDiscoveryLoaderSuccessDiagnostics(
      "sitemap",
      {
        totalEntryCount: 5,
        cmsEntryCount: 1,
        sitemapSummaryFootprint: "total:5|static:4|cms:1|products:0",
        sitemapCompositionFootprint: "static:4|cms:1|products:0",
      },
      {
        hasCanonicalCultureNormalization: false,
      },
    ),
    {
      localizedDiscoveryKind: "sitemap",
      localizedDiscoveryNormalization: "raw-cultures",
      localizedDiscoveryState: "present",
      localizedDiscoveryDetailFootprint: "static:4|cms:1|products:0",
      localizedDiscoverySummaryFootprint: "total:5|static:4|cms:1|products:0",
      totalEntryCount: 5,
      cmsEntryCount: 1,
      sitemapSummaryFootprint: "total:5|static:4|cms:1|products:0",
      sitemapCompositionFootprint: "static:4|cms:1|products:0",
    },
  );
});

test("buildLocalizedDiscoveryLoaderSuccessDiagnostics keeps raw sitemap empty mode explicit", () => {
  assert.deepEqual(
    buildLocalizedDiscoveryLoaderSuccessDiagnostics(
      "sitemap",
      {
        totalEntryCount: 0,
        cmsEntryCount: 0,
      },
      {
        hasCanonicalCultureNormalization: false,
      },
    ),
    {
      localizedDiscoveryKind: "sitemap",
      localizedDiscoveryNormalization: "raw-cultures",
      localizedDiscoveryState: "empty",
      localizedDiscoveryDetailFootprint: "detail:none",
      localizedDiscoverySummaryFootprint: "summary:none",
      totalEntryCount: 0,
      cmsEntryCount: 0,
    },
  );
});

test("buildLocalizedDiscoveryLoaderSuccessDiagnostics keeps raw inventory empty mode explicit", () => {
  assert.deepEqual(
    buildLocalizedDiscoveryLoaderSuccessDiagnostics(
      "inventory",
      {
        localizedCultureCount: 0,
        localizedItemCount: 0,
      },
      {
        hasCanonicalCultureNormalization: false,
      },
    ),
    {
      localizedDiscoveryKind: "inventory",
      localizedDiscoveryNormalization: "raw-cultures",
      localizedDiscoveryState: "empty",
      localizedDiscoveryDetailFootprint: "detail:none",
      localizedDiscoverySummaryFootprint: "summary:none",
      localizedCultureCount: 0,
      localizedItemCount: 0,
    },
  );
});
test("buildLocalizedDiscoveryLoaderSuccessDiagnostics keeps raw inventory unknown mode explicit", () => {
  assert.deepEqual(
    buildLocalizedDiscoveryLoaderSuccessDiagnostics(
      "inventory",
      {},
      {
        hasCanonicalCultureNormalization: false,
      },
    ),
    {
      localizedDiscoveryKind: "inventory",
      localizedDiscoveryNormalization: "raw-cultures",
      localizedDiscoveryState: "unknown",
      localizedDiscoveryDetailFootprint: "detail:none",
      localizedDiscoverySummaryFootprint: "summary:none",
    },
  );
});
test("buildLocalizedDiscoveryLoaderSuccessDiagnostics keeps canonical inventory unknown mode explicit", () => {
  assert.deepEqual(
    buildLocalizedDiscoveryLoaderSuccessDiagnostics(
      "inventory",
      {},
      {
        hasCanonicalCultureNormalization: true,
      },
    ),
    {
      localizedDiscoveryKind: "inventory",
      localizedDiscoveryNormalization: "canonical-cultures",
      localizedDiscoveryState: "unknown",
      localizedDiscoveryDetailFootprint: "detail:none",
      localizedDiscoverySummaryFootprint: "summary:none",
    },
  );
});
test("buildLocalizedDiscoveryLoaderSuccessDiagnostics keeps raw sitemap unknown mode explicit", () => {
  assert.deepEqual(
    buildLocalizedDiscoveryLoaderSuccessDiagnostics(
      "sitemap",
      {},
      {
        hasCanonicalCultureNormalization: false,
      },
    ),
    {
      localizedDiscoveryKind: "sitemap",
      localizedDiscoveryNormalization: "raw-cultures",
      localizedDiscoveryState: "unknown",
      localizedDiscoveryDetailFootprint: "detail:none",
      localizedDiscoverySummaryFootprint: "summary:none",
    },
  );
});
test("buildLocalizedDiscoveryLoaderSuccessDiagnostics keeps canonical sitemap unknown mode explicit", () => {
  assert.deepEqual(
    buildLocalizedDiscoveryLoaderSuccessDiagnostics(
      "sitemap",
      {},
      {
        hasCanonicalCultureNormalization: true,
      },
    ),
    {
      localizedDiscoveryKind: "sitemap",
      localizedDiscoveryNormalization: "canonical-cultures",
      localizedDiscoveryState: "unknown",
      localizedDiscoveryDetailFootprint: "detail:none",
      localizedDiscoverySummaryFootprint: "summary:none",
    },
  );
});
