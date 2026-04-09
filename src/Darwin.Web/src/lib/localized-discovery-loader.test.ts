import assert from "node:assert/strict";
import test from "node:test";
import {
  buildLocalizedDiscoveryLoaderDiagnostics,
  createLocalizedDiscoveryLoader,
} from "@/lib/localized-discovery-loader";

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
