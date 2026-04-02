import "server-only";
import { cookies } from "next/headers";
import type { PreparedMemberLoyaltyScanSession } from "@/features/member-portal/types";
import { isExpiredUtcTimestamp, isValidUtcTimestamp } from "@/lib/time";

const MEMBER_LOYALTY_SCAN_COOKIE = "darwin-member-loyalty-scan";

function isPreparedMemberLoyaltyScanSession(
  value: unknown,
): value is PreparedMemberLoyaltyScanSession {
  if (!value || typeof value !== "object") {
    return false;
  }

  const session = value as Record<string, unknown>;
  return (
    typeof session.businessId === "string" &&
    session.businessId.trim().length > 0 &&
    typeof session.scanSessionToken === "string" &&
    session.scanSessionToken.trim().length > 0 &&
    typeof session.expiresAtUtc === "string" &&
    isValidUtcTimestamp(session.expiresAtUtc) &&
    (session.businessLocationId === undefined ||
      session.businessLocationId === null ||
      typeof session.businessLocationId === "string") &&
    (session.mode === "Accrual" || session.mode === "Redemption") &&
    Array.isArray(session.selectedRewardTierIds)
  );
}

function getCookieBaseOptions() {
  return {
    httpOnly: true,
    sameSite: "lax" as const,
    path: "/",
    secure: process.env.NODE_ENV === "production",
    maxAge: 60 * 15,
  };
}

export async function readPreparedMemberLoyaltyScanSession(
  businessId: string,
): Promise<PreparedMemberLoyaltyScanSession | null> {
  const cookieStore = await cookies();
  const raw = cookieStore.get(MEMBER_LOYALTY_SCAN_COOKIE)?.value;
  if (!raw) {
    return null;
  }

  try {
    const parsed = JSON.parse(raw) as unknown;
    if (!isPreparedMemberLoyaltyScanSession(parsed) || parsed.businessId !== businessId) {
      cookieStore.delete(MEMBER_LOYALTY_SCAN_COOKIE);
      return null;
    }

    if (isExpiredUtcTimestamp(parsed.expiresAtUtc)) {
      cookieStore.delete(MEMBER_LOYALTY_SCAN_COOKIE);
      return null;
    }

    return parsed;
  } catch {
    cookieStore.delete(MEMBER_LOYALTY_SCAN_COOKIE);
    return null;
  }
}

export async function writePreparedMemberLoyaltyScanSession(
  session: PreparedMemberLoyaltyScanSession,
) {
  const cookieStore = await cookies();
  cookieStore.set(
    MEMBER_LOYALTY_SCAN_COOKIE,
    JSON.stringify(session),
    getCookieBaseOptions(),
  );
}

export async function clearPreparedMemberLoyaltyScanSession() {
  const cookieStore = await cookies();
  cookieStore.delete(MEMBER_LOYALTY_SCAN_COOKIE);
}
