"use server";

import { redirect } from "next/navigation";
import { loginMember, logoutMember } from "@/features/member-session/api/member-auth";
import {
  clearMemberSession,
  getMemberAccessToken,
  getMemberRefreshToken,
  writeMemberSession,
} from "@/features/member-session/cookies";
import { readNormalizedEmail, readTrimmedFormText } from "@/lib/form-data";
import { buildAppQueryPath, sanitizeAppPath } from "@/lib/locale-routing";
import { toLocalizedQueryMessage } from "@/localization";

export async function signInMemberAction(formData: FormData) {
  const email = readNormalizedEmail(formData);
  const password = readTrimmedFormText(formData, "password", 256);
  const returnPath = sanitizeAppPath(
    readTrimmedFormText(formData, "returnPath", 512) || "/account",
    "/account",
  );

  if (!email || !password) {
    redirect(
      buildAppQueryPath("/account/sign-in", {
        email,
        signInError: toLocalizedQueryMessage("signInCredentialsRequiredMessage"),
        returnPath,
      }),
    );
  }

  const result = await loginMember({
    email,
    password,
  });

  if (!result.data) {
    redirect(
      buildAppQueryPath("/account/sign-in", {
        email,
        signInError: result.message ?? toLocalizedQueryMessage("signInFailedMessage"),
        returnPath,
      }),
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

  redirect(returnPath);
}

export async function signOutMemberAction() {
  const [accessToken, refreshToken] = await Promise.all([
    getMemberAccessToken(),
    getMemberRefreshToken(),
  ]);

  if (accessToken && refreshToken) {
    await logoutMember({
      accessToken,
      refreshToken,
    });
  }

  await clearMemberSession();
  redirect("/account");
}
