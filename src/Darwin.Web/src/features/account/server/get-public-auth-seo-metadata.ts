import "server-only";
import { buildNoIndexMetadata } from "@/lib/seo";
import { createCachedObservedSeoMetadataLoader } from "@/lib/seo-loader";
import { getMemberResource } from "@/localization";

type PublicAuthRoute =
  | "/account"
  | "/account/sign-in"
  | "/account/register"
  | "/account/activation"
  | "/account/password";

function getRouteTitle(culture: string, route: PublicAuthRoute) {
  const copy = getMemberResource(culture);

  switch (route) {
    case "/account":
      return copy.accountMetaTitle;
    case "/account/sign-in":
      return copy.signInMetaTitle;
    case "/account/register":
      return copy.registerMetaTitle;
    case "/account/activation":
      return copy.activationMetaTitle;
    case "/account/password":
      return copy.passwordMetaTitle;
  }
}

function normalizePublicAuthSeoArgs(
  culture: string,
  route: PublicAuthRoute,
): [string, PublicAuthRoute] {
  return [culture.trim(), route.trim() as PublicAuthRoute];
}

const getCachedPublicAuthSeoMetadata = createCachedObservedSeoMetadataLoader({
  area: "public-auth-seo",
  operation: "load-route-seo-metadata",
  thresholdMs: 150,
  normalizeArgs: normalizePublicAuthSeoArgs,
  getContext: (culture: string, route: PublicAuthRoute) => ({
    culture,
    route,
  }),
  load: async (culture: string, route: PublicAuthRoute) => ({
    metadata: buildNoIndexMetadata(culture, getRouteTitle(culture, route), undefined, route),
    canonicalPath: route,
    noIndex: true,
    languageAlternates: {},
  }),
});

export function getPublicAccountSeoMetadata(culture: string) {
  return getCachedPublicAuthSeoMetadata(culture, "/account");
}

export function getPublicActivationSeoMetadata(culture: string) {
  return getCachedPublicAuthSeoMetadata(culture, "/account/activation");
}

export function getPublicPasswordSeoMetadata(culture: string) {
  return getCachedPublicAuthSeoMetadata(culture, "/account/password");
}

export function getPublicRegisterSeoMetadata(culture: string) {
  return getCachedPublicAuthSeoMetadata(culture, "/account/register");
}

export function getPublicSignInSeoMetadata(culture: string) {
  return getCachedPublicAuthSeoMetadata(culture, "/account/sign-in");
}
