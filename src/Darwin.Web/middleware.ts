import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

export function middleware(request: NextRequest) {
  const runtimeConfig = getSiteRuntimeConfig();
  const culture = request.nextUrl.searchParams.get("culture");

  if (!culture || !runtimeConfig.supportedCultures.includes(culture)) {
    return NextResponse.next();
  }

  const redirectUrl = request.nextUrl.clone();
  redirectUrl.searchParams.delete("culture");

  const response = NextResponse.redirect(redirectUrl);
  response.cookies.set(runtimeConfig.cultureCookieName, culture, {
    path: "/",
    sameSite: "lax",
    maxAge: 60 * 60 * 24 * 365,
  });

  return response;
}

export const config = {
  matcher: ["/((?!api|_next/static|_next/image|favicon.ico).*)"],
};
