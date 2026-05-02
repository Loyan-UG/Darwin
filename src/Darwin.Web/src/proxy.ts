import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import {
  buildLocalizedPath,
  INFERRED_CULTURE_SEARCH_PARAM,
  isPublicLocalizedPath,
  REQUEST_CULTURE_HEADER,
  REQUEST_PATHNAME_HEADER,
  stripCulturePrefix,
} from "@/lib/locale-routing";
import {
  getFallbackPublicSiteRuntimeConfig,
  isPublicSiteRuntimeConfig,
  normalizePublicSiteRuntimeConfig,
  type PublicSiteRuntimeConfig,
} from "@/lib/public-site-runtime-config-shared";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

const PUBLIC_RUNTIME_CONFIG_CACHE_MS = 60_000;
const PUBLIC_RUNTIME_CONFIG_PATH = "/api/v1/public/site/runtime-config";

let cachedPublicRuntimeConfig:
  | {
      expiresAt: number;
      value: PublicSiteRuntimeConfig;
    }
  | undefined;
let pendingPublicRuntimeConfig:
  | Promise<PublicSiteRuntimeConfig>
  | undefined;

function buildPublicRuntimeConfigUrl() {
  const runtimeConfig = getSiteRuntimeConfig();
  return new URL(PUBLIC_RUNTIME_CONFIG_PATH, `${runtimeConfig.webApiBaseUrl}/`)
    .toString();
}

async function fetchPublicRuntimeConfig() {
  try {
    const response = await fetch(buildPublicRuntimeConfigUrl(), {
      headers: {
        Accept: "application/json",
      },
      cache: "no-store",
    });

    if (!response.ok) {
      return getFallbackPublicSiteRuntimeConfig();
    }

    const payload: unknown = await response.json();
    return isPublicSiteRuntimeConfig(payload)
      ? normalizePublicSiteRuntimeConfig(payload)
      : getFallbackPublicSiteRuntimeConfig();
  } catch {
    return getFallbackPublicSiteRuntimeConfig();
  }
}

async function getProxyPublicRuntimeConfig() {
  const now = Date.now();
  if (cachedPublicRuntimeConfig && cachedPublicRuntimeConfig.expiresAt > now) {
    return cachedPublicRuntimeConfig.value;
  }

  pendingPublicRuntimeConfig ??= fetchPublicRuntimeConfig().finally(() => {
    pendingPublicRuntimeConfig = undefined;
  });

  const value = await pendingPublicRuntimeConfig;
  cachedPublicRuntimeConfig = {
    expiresAt: now + PUBLIC_RUNTIME_CONFIG_CACHE_MS,
    value,
  };

  return value;
}

export async function proxy(request: NextRequest) {
  const runtimeConfig = getSiteRuntimeConfig();
  const publicRuntimeConfig = await getProxyPublicRuntimeConfig();
  const supportedCultures = publicRuntimeConfig.supportedCultures;
  const defaultCulture = publicRuntimeConfig.defaultCulture;
  const searchParamCulture = request.nextUrl.searchParams.get("culture");
  const cookieCulture = request.cookies.get(runtimeConfig.cultureCookieName)?.value;
  const pathnameContext = stripCulturePrefix(
    request.nextUrl.pathname,
    supportedCultures,
  );
  const hasCulturePrefix = Boolean(pathnameContext.culture);
  const pathCulture =
    pathnameContext.culture &&
    supportedCultures.includes(pathnameContext.culture)
      ? pathnameContext.culture
      : null;
  const validSearchCulture =
    searchParamCulture &&
    supportedCultures.includes(searchParamCulture)
      ? searchParamCulture
      : null;
  const validCookieCulture =
    cookieCulture && supportedCultures.includes(cookieCulture)
      ? cookieCulture
      : null;
  const effectiveCulture =
    validSearchCulture ??
    pathCulture ??
    validCookieCulture ??
    defaultCulture;
  const isInferredCultureRequest =
    validSearchCulture &&
    request.nextUrl.searchParams.get(INFERRED_CULTURE_SEARCH_PARAM) === "1";

  if (validSearchCulture && !isInferredCultureRequest) {
    const redirectUrl = request.nextUrl.clone();
    redirectUrl.searchParams.delete("culture");
    redirectUrl.pathname = isPublicLocalizedPath(pathnameContext.pathname)
      ? buildLocalizedPath(pathnameContext.pathname, validSearchCulture)
      : pathnameContext.pathname;

    const response = NextResponse.redirect(redirectUrl);
    response.cookies.set(runtimeConfig.cultureCookieName, validSearchCulture, {
      path: "/",
      sameSite: "lax",
      maxAge: 60 * 60 * 24 * 365,
    });

    return response;
  }

  if (hasCulturePrefix) {
    const prefixedCulture = pathCulture ?? defaultCulture;
    const redirectUrl = request.nextUrl.clone();
    redirectUrl.pathname = pathnameContext.pathname;

    const response = NextResponse.redirect(redirectUrl);
    response.cookies.set(runtimeConfig.cultureCookieName, prefixedCulture, {
      path: "/",
      sameSite: "lax",
      maxAge: 60 * 60 * 24 * 365,
    });

    return response;
  }

  const requestHeaders = new Headers(request.headers);
  requestHeaders.set(REQUEST_CULTURE_HEADER, effectiveCulture);
  requestHeaders.set(REQUEST_PATHNAME_HEADER, pathnameContext.pathname);

  const response = NextResponse.next({
    request: {
      headers: requestHeaders,
    },
  });

  if (validCookieCulture !== effectiveCulture) {
    response.cookies.set(runtimeConfig.cultureCookieName, effectiveCulture, {
      path: "/",
      sameSite: "lax",
      maxAge: 60 * 60 * 24 * 365,
    });
  }

  return response;
}

export const config = {
  matcher: ["/((?!api|_next/static|_next/image|favicon.ico).*)"],
};
