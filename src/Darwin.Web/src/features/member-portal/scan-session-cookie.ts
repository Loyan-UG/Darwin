import "server-only";
import { cookies } from "next/headers";
import type { PreparedMemberLoyaltyScanSession } from "@/features/member-portal/types";

const MEMBER_LOYALTY_SCAN_COOKIE = "darwin-member-loyalty-scan";

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
    const parsed = JSON.parse(raw) as PreparedMemberLoyaltyScanSession;
    if (
      !parsed.businessId ||
      !parsed.scanSessionToken ||
      parsed.businessId !== businessId
    ) {
      cookieStore.delete(MEMBER_LOYALTY_SCAN_COOKIE);
      return null;
    }

    const expiresAt = new Date(parsed.expiresAtUtc).getTime();
    if (!Number.isFinite(expiresAt) || expiresAt <= Date.now()) {
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
