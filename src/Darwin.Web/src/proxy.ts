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
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

export function proxy(request: NextRequest) {
  const runtimeConfig = getSiteRuntimeConfig();
  const searchParamCulture = request.nextUrl.searchParams.get("culture");
  const cookieCulture = request.cookies.get(runtimeConfig.cultureCookieName)?.value;
  const pathnameContext = stripCulturePrefix(request.nextUrl.pathname);
  const hasCulturePrefix = Boolean(pathnameContext.culture);
  const pathCulture =
    pathnameContext.culture &&
    runtimeConfig.supportedCultures.includes(pathnameContext.culture)
      ? pathnameContext.culture
      : null;
  const validSearchCulture =
    searchParamCulture &&
    runtimeConfig.supportedCultures.includes(searchParamCulture)
      ? searchParamCulture
      : null;
  const validCookieCulture =
    cookieCulture && runtimeConfig.supportedCultures.includes(cookieCulture)
      ? cookieCulture
      : null;
  const effectiveCulture =
    validSearchCulture ??
    pathCulture ??
    validCookieCulture ??
    runtimeConfig.defaultCulture;
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
    const prefixedCulture = pathCulture ?? runtimeConfig.defaultCulture;
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
