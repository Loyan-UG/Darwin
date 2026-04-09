export type SharedContextKind =
  | "member-summary"
  | "storefront-continuation"
  | "public-storefront"
  | "member-entry";

export function buildSharedContextBaseDiagnostics(
  kind: SharedContextKind,
  options?: {
    hasCanonicalNormalization?: boolean;
    extras?: Record<string, unknown>;
  },
) {
  return {
    sharedContextKind: kind,
    sharedContextNormalization: options?.hasCanonicalNormalization
      ? "canonical"
      : "raw",
    ...(options?.extras ?? {}),
  };
}

export function buildStorefrontContinuationFootprint(input: {
  cmsStatus: string;
  categoriesStatus: string;
  productsStatus: string;
}) {
  return `cms:${input.cmsStatus}|categories:${input.categoriesStatus}|products:${input.productsStatus}`;
}

export function buildPublicStorefrontFootprint(input: {
  cmsStatus: string;
  categoriesStatus: string;
  productsStatus: string;
  cartStatus: string;
}) {
  return `cms:${input.cmsStatus}|categories:${input.categoriesStatus}|products:${input.productsStatus}|cart:${input.cartStatus}`;
}

export function buildMemberEntryFootprint(input: {
  sessionState: "present" | "missing";
  storefrontState: "present" | "missing";
}) {
  return `session:${input.sessionState}|storefront:${input.storefrontState}`;
}

export function buildMemberSummaryFootprint(input: {
  scope: "identity" | "commerce-summary" | "orders-page" | "invoices-page";
  primaryStatus: string;
  secondaryStatus?: string;
  tertiaryStatus?: string;
}) {
  return [
    `scope:${input.scope}`,
    `primary:${input.primaryStatus}`,
    input.secondaryStatus ? `secondary:${input.secondaryStatus}` : null,
    input.tertiaryStatus ? `tertiary:${input.tertiaryStatus}` : null,
  ].filter(Boolean).join("|");
}
