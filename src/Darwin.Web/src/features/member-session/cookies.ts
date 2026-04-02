import "server-only";
import { cookies } from "next/headers";
import type { MemberSession } from "@/features/member-session/types";
import { isValidUtcTimestamp } from "@/lib/time";

const ACCESS_TOKEN_COOKIE = "darwin-member-access-token";
const REFRESH_TOKEN_COOKIE = "darwin-member-refresh-token";
const SESSION_COOKIE = "darwin-member-session";

function isMemberSession(value: unknown): value is MemberSession {
  if (!value || typeof value !== "object") {
    return false;
  }

  const session = value as Record<string, unknown>;
  return (
    typeof session.userId === "string" &&
    session.userId.trim().length > 0 &&
    typeof session.email === "string" &&
    session.email.includes("@") &&
    typeof session.accessTokenExpiresAtUtc === "string" &&
    isValidUtcTimestamp(session.accessTokenExpiresAtUtc)
  );
}

function getCookieBaseOptions() {
  return {
    httpOnly: true,
    sameSite: "lax" as const,
    path: "/",
    secure: process.env.NODE_ENV === "production",
    maxAge: 60 * 60 * 24 * 14,
  };
}

export async function getMemberSession() {
  const cookieStore = await cookies();
  const raw = cookieStore.get(SESSION_COOKIE)?.value;
  if (!raw) {
    return null;
  }

  try {
    const parsed = JSON.parse(raw) as unknown;
    return isMemberSession(parsed) ? parsed : null;
  } catch {
    return null;
  }
}

export async function getMemberAccessToken() {
  const cookieStore = await cookies();
  return cookieStore.get(ACCESS_TOKEN_COOKIE)?.value ?? null;
}

export async function getMemberRefreshToken() {
  const cookieStore = await cookies();
  return cookieStore.get(REFRESH_TOKEN_COOKIE)?.value ?? null;
}

export async function writeMemberSession(input: {
  accessToken: string;
  refreshToken: string;
  session: MemberSession;
}) {
  const cookieStore = await cookies();
  const options = getCookieBaseOptions();
  cookieStore.set(ACCESS_TOKEN_COOKIE, input.accessToken, options);
  cookieStore.set(REFRESH_TOKEN_COOKIE, input.refreshToken, options);
  cookieStore.set(SESSION_COOKIE, JSON.stringify(input.session), options);
}

export async function clearMemberSession() {
  const cookieStore = await cookies();
  cookieStore.delete(ACCESS_TOKEN_COOKIE);
  cookieStore.delete(REFRESH_TOKEN_COOKIE);
  cookieStore.delete(SESSION_COOKIE);
}
