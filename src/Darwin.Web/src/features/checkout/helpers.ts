import type { CheckoutDraft, PublicCheckoutAddress } from "@/features/checkout/types";

type SearchParamValue = string | string[] | undefined;
type DraftValue = FormDataEntryValue | SearchParamValue | null;

const DEFAULT_COUNTRY_CODE = "DE";

function normalizeValue(value: DraftValue) {
  if (Array.isArray(value)) {
    return String(value[0] ?? "").trim();
  }

  return String(value ?? "").trim();
}

function createEmptyDraft(): CheckoutDraft {
  return {
    fullName: "",
    company: "",
    street1: "",
    street2: "",
    postalCode: "",
    city: "",
    state: "",
    countryCode: DEFAULT_COUNTRY_CODE,
    phoneE164: "",
    selectedShippingMethodId: "",
  };
}

export function readCheckoutDraftFromSearchParams(
  searchParams?: Record<string, SearchParamValue>,
): CheckoutDraft {
  const draft = createEmptyDraft();
  if (!searchParams) {
    return draft;
  }

  return {
    fullName: normalizeValue(searchParams.fullName),
    company: normalizeValue(searchParams.company),
    street1: normalizeValue(searchParams.street1),
    street2: normalizeValue(searchParams.street2),
    postalCode: normalizeValue(searchParams.postalCode),
    city: normalizeValue(searchParams.city),
    state: normalizeValue(searchParams.state),
    countryCode: normalizeValue(searchParams.countryCode) || DEFAULT_COUNTRY_CODE,
    phoneE164: normalizeValue(searchParams.phoneE164),
    selectedShippingMethodId: normalizeValue(searchParams.selectedShippingMethodId),
  };
}

export function readCheckoutDraftFromFormData(formData: FormData): CheckoutDraft {
  return {
    fullName: normalizeValue(formData.get("fullName")),
    company: normalizeValue(formData.get("company")),
    street1: normalizeValue(formData.get("street1")),
    street2: normalizeValue(formData.get("street2")),
    postalCode: normalizeValue(formData.get("postalCode")),
    city: normalizeValue(formData.get("city")),
    state: normalizeValue(formData.get("state")),
    countryCode: normalizeValue(formData.get("countryCode")) || DEFAULT_COUNTRY_CODE,
    phoneE164: normalizeValue(formData.get("phoneE164")),
    selectedShippingMethodId: normalizeValue(formData.get("selectedShippingMethodId")),
  };
}

export function isCheckoutAddressComplete(draft: CheckoutDraft) {
  return Boolean(
    draft.fullName &&
      draft.street1 &&
      draft.postalCode &&
      draft.city &&
      draft.countryCode,
  );
}

export function toCheckoutAddress(draft: CheckoutDraft): PublicCheckoutAddress {
  return {
    fullName: draft.fullName,
    company: draft.company || null,
    street1: draft.street1,
    street2: draft.street2 || null,
    postalCode: draft.postalCode,
    city: draft.city,
    state: draft.state || null,
    countryCode: draft.countryCode || DEFAULT_COUNTRY_CODE,
    phoneE164: draft.phoneE164 || null,
  };
}

export function buildCheckoutDraftSearch(
  draft: CheckoutDraft,
  extras?: Record<string, string | undefined>,
) {
  const params = new URLSearchParams();

  for (const [key, value] of Object.entries(draft)) {
    if (value) {
      params.set(key, value);
    }
  }

  if (extras) {
    for (const [key, value] of Object.entries(extras)) {
      if (value) {
        params.set(key, value);
      }
    }
  }

  const serialized = params.toString();
  return serialized ? `?${serialized}` : "";
}

export function readSingleSearchParam(value: SearchParamValue) {
  return normalizeValue(value) || undefined;
}
