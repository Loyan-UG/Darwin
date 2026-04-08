type PageLoaderKind =
  | "public-discovery"
  | "member-protected"
  | "commerce";

export function buildPageLoaderBaseDiagnostics(
  kind: PageLoaderKind,
  options?: {
    hasCanonicalNormalization?: boolean;
    extras?: Record<string, unknown>;
  },
) {
  return {
    pageLoaderKind: kind,
    pageLoaderNormalization: options?.hasCanonicalNormalization
      ? "canonical"
      : "raw",
    ...(options?.extras ?? {}),
  };
}

export function buildContinuationSliceFootprint(input: {
  cmsCount: number;
  categoryCount: number;
  productCount: number;
  cartState: "present" | "missing";
}) {
  return `cms:${input.cmsCount}|categories:${input.categoryCount}|products:${input.productCount}|cart:${input.cartState}`;
}

export function buildProtectedRouteFootprint(input: {
  authGate: "authorized" | "guest-fallback";
  routeContextState: "loaded" | "guest-fallback";
  storefrontFallbackState: "present" | "missing";
}) {
  return `auth:${input.authGate}|route:${input.routeContextState}|storefront:${input.storefrontFallbackState}`;
}
