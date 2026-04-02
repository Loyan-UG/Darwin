export type ParsedAddress = {
  fullName?: string;
  company?: string | null;
  street1?: string;
  street2?: string | null;
  postalCode?: string;
  city?: string;
  state?: string | null;
  countryCode?: string;
  phoneE164?: string | null;
};

export function parseAddressJson(rawJson: string): ParsedAddress | null {
  try {
    const parsed = JSON.parse(rawJson) as unknown;
    if (!parsed || typeof parsed !== "object") {
      return null;
    }

    const address = parsed as Record<string, unknown>;
    return {
      fullName: typeof address.fullName === "string" ? address.fullName : undefined,
      company: typeof address.company === "string" ? address.company : null,
      street1: typeof address.street1 === "string" ? address.street1 : undefined,
      street2: typeof address.street2 === "string" ? address.street2 : null,
      postalCode:
        typeof address.postalCode === "string" ? address.postalCode : undefined,
      city: typeof address.city === "string" ? address.city : undefined,
      state: typeof address.state === "string" ? address.state : null,
      countryCode:
        typeof address.countryCode === "string"
          ? address.countryCode
          : undefined,
      phoneE164:
        typeof address.phoneE164 === "string" ? address.phoneE164 : null,
    };
  } catch {
    return null;
  }
}
