import assert from "node:assert/strict";
import test from "node:test";
import { normalizePublishedPageSetInput } from "@/features/cms/api/public-cms-set-input";

test("normalizePublishedPageSetInput trims cache keys down to stable values", () => {
  assert.deepEqual(
    normalizePublishedPageSetInput({
      culture: " de-DE ",
      search: "  willkommen  ",
    }),
    {
      culture: "de-DE",
      search: "willkommen",
    },
  );

  assert.deepEqual(
    normalizePublishedPageSetInput({
      culture: "   ",
      search: "",
    }),
    {
      culture: undefined,
      search: undefined,
    },
  );
});
