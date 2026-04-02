import "server-only";
import { clearMemberSession, getMemberAccessToken, getMemberRefreshToken, getMemberSession, writeMemberSession } from "@/features/member-session/cookies";
import { refreshMember } from "@/features/member-session/api/member-auth";
import { parseUtcTimestamp } from "@/lib/time";

function expiresSoon(expiresAtUtc: string) {
  const value = parseUtcTimestamp(expiresAtUtc);
  if (value === null) {
    return true;
  }

  return value <= Date.now() + 60_000;
}

export async function getFreshMemberAccessToken(forceRefresh = false) {
  const [session, accessToken, refreshToken] = await Promise.all([
    getMemberSession(),
    getMemberAccessToken(),
    getMemberRefreshToken(),
  ]);

  if (!session || !accessToken || !refreshToken) {
    return null;
  }

  if (!forceRefresh && !expiresSoon(session.accessTokenExpiresAtUtc)) {
    return accessToken;
  }

  const refreshResult = await refreshMember({
    refreshToken,
  });

  if (!refreshResult.data) {
    await clearMemberSession();
    return null;
  }

  await writeMemberSession({
    accessToken: refreshResult.data.accessToken,
    refreshToken: refreshResult.data.refreshToken,
    session: {
      userId: refreshResult.data.userId,
      email: refreshResult.data.email,
      accessTokenExpiresAtUtc: refreshResult.data.accessTokenExpiresAtUtc,
    },
  });

  return refreshResult.data.accessToken;
}
