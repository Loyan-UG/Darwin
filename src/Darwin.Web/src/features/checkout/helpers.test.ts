import test from "node:test";
import assert from "node:assert/strict";
import {
  buildCheckoutDraftSearch,
  mergeCheckoutDraft,
  readBoundedNumericSearchParam,
  readCheckoutDraftFromSearchParams,
  readNonNegativeIntegerFromFormData,
  readPositiveIntegerSearchParam,
  readSearchTextParam,
  toCheckoutDraftFromMemberProfile,
} from "@/features/checkout/helpers";

test("readCheckoutDraftFromSearchParams normalizes visible checkout fields", () => {
  const draft = readCheckoutDraftFromSearchParams({
    fullName: "  Ada Lovelace  ",
    city: " Berlin ",
    countryCode: "de",
    phoneE164: " +49123 ",
    selectedShippingMethodId: " express ",
  });

  assert.deepEqual(draft, {
    fullName: "Ada Lovelace",
    company: "",
    street1: "",
    street2: "",
    postalCode: "",
    city: "Berlin",
    state: "",
    countryCode: "DE",
    phoneE164: "+49123",
    selectedShippingMethodId: "express",
  });
});

test("mergeCheckoutDraft keeps the shopper-entered fields and fills the gaps", () => {
  const merged = mergeCheckoutDraft(
    {
      fullName: "",
      company: "",
      street1: "Main Street 1",
      street2: "",
      postalCode: "",
      city: "",
      state: "",
      countryCode: "DE",
      phoneE164: "",
      selectedShippingMethodId: "",
    },
    {
      fullName: "Ada Lovelace",
      city: "Berlin",
      postalCode: "10115",
      phoneE164: "+49123",
    },
  );

  assert.equal(merged.fullName, "Ada Lovelace");
  assert.equal(merged.street1, "Main Street 1");
  assert.equal(merged.city, "Berlin");
  assert.equal(merged.postalCode, "10115");
  assert.equal(merged.phoneE164, "+49123");
});

test("buildCheckoutDraftSearch emits canonical query strings for reusable draft handoff", () => {
  const query = buildCheckoutDraftSearch({
    fullName: "Ada Lovelace",
    company: "",
    street1: "Main Street 1",
    street2: "",
    postalCode: "10115",
    city: "Berlin",
    state: "",
    countryCode: "DE",
    phoneE164: "+49123",
    selectedShippingMethodId: "",
  });

  assert.equal(
    query,
    "?fullName=Ada+Lovelace&street1=Main+Street+1&postalCode=10115&city=Berlin&countryCode=DE&phoneE164=%2B49123",
  );
});

test("positive and bounded search param readers reject malformed values", () => {
  assert.equal(readPositiveIntegerSearchParam("4"), 4);
  assert.equal(readPositiveIntegerSearchParam("0"), 1);
  assert.equal(readPositiveIntegerSearchParam("3.5"), 1);

  assert.equal(
    readBoundedNumericSearchParam("48.137", { min: -90, max: 90 }),
    48.137,
  );
  assert.equal(
    readBoundedNumericSearchParam("48.137.1", { min: -90, max: 90 }),
    undefined,
  );
  assert.equal(
    readBoundedNumericSearchParam("120", { min: -90, max: 90 }),
    undefined,
  );
});

test("text and form readers trim and reject invalid values", () => {
  assert.equal(readSearchTextParam("  spring offers  "), "spring offers");
  assert.equal(readSearchTextParam(""), undefined);

  const formData = new FormData();
  formData.set("shippingTotalMinor", "1200");
  formData.set("badTotal", "12.5");

  assert.equal(
    readNonNegativeIntegerFromFormData(formData, "shippingTotalMinor"),
    1200,
  );
  assert.equal(readNonNegativeIntegerFromFormData(formData, "badTotal"), null);
});

test("member profile prefill derives a clean checkout identity snapshot", () => {
  const draft = toCheckoutDraftFromMemberProfile({
    id: "member-1",
    email: "ada@example.com",
    firstName: " Ada ",
    lastName: " Lovelace ",
    phoneE164: "+49123",
    isPhoneVerified: true,
    preferredCulture: "de-DE",
    preferredCurrency: "EUR",
    preferredTimeZone: "Europe/Berlin",
  });

  assert.equal(draft.fullName, "Ada Lovelace");
  assert.equal(draft.phoneE164, "+49123");
  assert.equal(draft.countryCode, "DE");
});
