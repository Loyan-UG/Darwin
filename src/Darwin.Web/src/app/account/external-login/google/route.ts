import { NextResponse, type NextRequest } from "next/server";
import { loginMemberWithExternalProvider } from "@/features/member-session/api/member-auth";
import { writeMemberSession } from "@/features/member-session/cookies";
import { sanitizeAppPath } from "@/lib/locale-routing";
import { toLocalizedQueryMessage } from "@/localization";

type GoogleExternalLoginPayload = {
  credential?: unknown;
  returnPath?: unknown;
};

function getString(value: unknown) {
  return typeof value === "string" ? value.trim() : "";
}

export async function POST(request: NextRequest) {
  let payload: GoogleExternalLoginPayload;

  try {
    payload = (await request.json()) as GoogleExternalLoginPayload;
  } catch {
    return NextResponse.json(
      {
        ok: false,
        message: toLocalizedQueryMessage("externalLoginFailedMessage"),
      },
      { status: 400 },
    );
  }

  const credential = getString(payload.credential);
  const returnPath = sanitizeAppPath(getString(payload.returnPath), "/account");

  if (!credential) {
    return NextResponse.json(
      {
        ok: false,
        message: toLocalizedQueryMessage("externalLoginMissingCredentialMessage"),
      },
      { status: 400 },
    );
  }

  const result = await loginMemberWithExternalProvider({
    provider: "Google",
    identityToken: credential,
    deviceId: request.headers.get("user-agent")?.slice(0, 128) || undefined,
  });

  if (!result.data) {
    return NextResponse.json(
      {
        ok: false,
        message: result.message ?? toLocalizedQueryMessage("externalLoginFailedMessage"),
      },
      { status: 401 },
    );
  }

  await writeMemberSession({
    accessToken: result.data.accessToken,
    refreshToken: result.data.refreshToken,
    session: {
      userId: result.data.userId,
      email: result.data.email,
      accessTokenExpiresAtUtc: result.data.accessTokenExpiresAtUtc,
    },
  });

  return NextResponse.json({
    ok: true,
    redirectTo: returnPath,
  });
}
