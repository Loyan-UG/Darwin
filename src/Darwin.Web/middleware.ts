import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import {
  buildLocalizedPath,
  isPublicLocalizedPath,
  REQUEST_CULTURE_HEADER,
  stripCulturePrefix,
} from "@/lib/locale-routing";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

export function middleware(request: NextRequest) {
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

  if (validSearchCulture) {
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

    if (
      prefixedCulture === runtimeConfig.defaultCulture ||
      !isPublicLocalizedPath(pathnameContext.pathname)
    ) {
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

    const rewrittenUrl = request.nextUrl.clone();
    rewrittenUrl.pathname = pathnameContext.pathname;
    const requestHeaders = new Headers(request.headers);
    requestHeaders.set(REQUEST_CULTURE_HEADER, prefixedCulture);

    const response = NextResponse.rewrite(rewrittenUrl, {
      request: {
        headers: requestHeaders,
      },
    });
    response.cookies.set(runtimeConfig.cultureCookieName, prefixedCulture, {
      path: "/",
      sameSite: "lax",
      maxAge: 60 * 60 * 24 * 365,
    });

    return response;
  }

  if (
    effectiveCulture !== runtimeConfig.defaultCulture &&
    isPublicLocalizedPath(request.nextUrl.pathname) &&
    (request.method === "GET" || request.method === "HEAD")
  ) {
    const redirectUrl = request.nextUrl.clone();
    redirectUrl.pathname = buildLocalizedPath(
      request.nextUrl.pathname,
      effectiveCulture,
    );

    const response = NextResponse.redirect(redirectUrl);
    response.cookies.set(runtimeConfig.cultureCookieName, effectiveCulture, {
      path: "/",
      sameSite: "lax",
      maxAge: 60 * 60 * 24 * 365,
    });

    return response;
  }

  const requestHeaders = new Headers(request.headers);
  requestHeaders.set(REQUEST_CULTURE_HEADER, effectiveCulture);

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
