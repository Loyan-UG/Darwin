import assert from "node:assert/strict";
import test from "node:test";
import { normalizePublicProductSetInput } from "@/features/catalog/api/public-catalog-set-input";

test("normalizePublicProductSetInput trims cache keys down to stable values", () => {
  assert.deepEqual(
    normalizePublicProductSetInput({
      culture: " en-US ",
      categorySlug: "  getraenke ",
      search: "  cola ",
    }),
    {
      culture: "en-US",
      categorySlug: "getraenke",
      search: "cola",
    },
  );

  assert.deepEqual(
    normalizePublicProductSetInput({
      culture: "",
      categorySlug: "   ",
      search: "   ",
    }),
    {
      culture: undefined,
      categorySlug: undefined,
      search: undefined,
    },
  );
});
