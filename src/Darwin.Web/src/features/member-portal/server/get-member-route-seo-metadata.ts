import "server-only";
import { buildInvoicePath, buildOrderPath } from "@/lib/entity-paths";
import { normalizeEntityRouteArgs } from "@/lib/route-context-normalization";
import { buildNoIndexMetadata } from "@/lib/seo";
import { createCachedObservedSeoMetadataLoader } from "@/lib/seo-loader";
import { getMemberResource } from "@/localization";

type MemberSeoRoute =
  | "/account/profile"
  | "/account/preferences"
  | "/account/addresses"
  | "/account/security"
  | "/orders"
  | "/invoices"
  | "/orders/[id]"
  | "/invoices/[id]";

function getRouteTitle(culture: string, route: MemberSeoRoute) {
  const copy = getMemberResource(culture);

  switch (route) {
    case "/account/profile":
      return copy.profileMetaTitle;
    case "/account/preferences":
      return copy.preferencesMetaTitle;
    case "/account/addresses":
      return copy.addressesMetaTitle;
    case "/account/security":
      return copy.securityMetaTitle;
    case "/orders":
      return copy.ordersMetaTitle;
    case "/invoices":
      return copy.invoicesMetaTitle;
    case "/orders/[id]":
      return copy.orderDetailMetaTitle;
    case "/invoices/[id]":
      return copy.invoiceDetailMetaTitle;
  }
}

function normalizeMemberSeoArgs(
  culture: string,
  route: MemberSeoRoute,
  canonicalPath: string,
): [string, MemberSeoRoute, string] {
  const [normalizedCulture, normalizedCanonicalPath] = normalizeEntityRouteArgs(
    culture,
    canonicalPath,
  );

  return [normalizedCulture, route.trim() as MemberSeoRoute, normalizedCanonicalPath];
}

const getCachedMemberRouteSeoMetadata = createCachedObservedSeoMetadataLoader({
  area: "member-route-seo",
  operation: "load-route-seo-metadata",
  thresholdMs: 150,
  normalizeArgs: normalizeMemberSeoArgs,
  getContext: (
    culture: string,
    route: MemberSeoRoute,
    canonicalPath: string,
  ) => ({
    culture,
    route,
    canonicalPath,
  }),
  load: async (culture: string, route: MemberSeoRoute, canonicalPath: string) => ({
    metadata: buildNoIndexMetadata(
      culture,
      getRouteTitle(culture, route),
      undefined,
      canonicalPath,
    ),
    canonicalPath,
    noIndex: true,
    languageAlternates: {},
  }),
});

export const getProfileSeoMetadata = (culture: string) =>
  getCachedMemberRouteSeoMetadata(
    culture,
    "/account/profile",
    "/account/profile",
  );

export const getPreferencesSeoMetadata = (culture: string) =>
  getCachedMemberRouteSeoMetadata(
    culture,
    "/account/preferences",
    "/account/preferences",
  );

export const getAddressesSeoMetadata = (culture: string) =>
  getCachedMemberRouteSeoMetadata(
    culture,
    "/account/addresses",
    "/account/addresses",
  );

export const getSecuritySeoMetadata = (culture: string) =>
  getCachedMemberRouteSeoMetadata(
    culture,
    "/account/security",
    "/account/security",
  );

export const getOrdersSeoMetadata = (culture: string) =>
  getCachedMemberRouteSeoMetadata(culture, "/orders", "/orders");

export const getInvoicesSeoMetadata = (culture: string) =>
  getCachedMemberRouteSeoMetadata(culture, "/invoices", "/invoices");

export const getOrderDetailSeoMetadata = (culture: string, id: string) =>
  getCachedMemberRouteSeoMetadata(
    culture,
    "/orders/[id]",
    buildOrderPath(id),
  );

export const getInvoiceDetailSeoMetadata = (culture: string, id: string) =>
  getCachedMemberRouteSeoMetadata(
    culture,
    "/invoices/[id]",
    buildInvoicePath(id),
  );
