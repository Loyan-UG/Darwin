"use server";

import { redirect } from "next/navigation";
import { loginMember, logoutMember } from "@/features/member-session/api/member-auth";
import {
  clearMemberSession,
  getMemberAccessToken,
  getMemberRefreshToken,
  writeMemberSession,
} from "@/features/member-session/cookies";
import { sanitizeAppPath } from "@/lib/locale-routing";
import { toLocalizedQueryMessage } from "@/localization";

function buildSearch(values: Record<string, string | undefined>) {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(values)) {
    if (value) {
      params.set(key, value);
    }
  }

  const query = params.toString();
  return query ? `?${query}` : "";
}

function normalizeEmail(value: FormDataEntryValue | null) {
  return String(value ?? "").trim().toLowerCase();
}

export async function signInMemberAction(formData: FormData) {
  const email = normalizeEmail(formData.get("email"));
  const password = String(formData.get("password") ?? "").trim();
  const returnPath = sanitizeAppPath(
    String(formData.get("returnPath") ?? "/account"),
    "/account",
  );

  if (!email || !password) {
    redirect(
      `/account/sign-in${buildSearch({
        email,
        signInError: toLocalizedQueryMessage("signInCredentialsRequiredMessage"),
        returnPath,
      })}`,
    );
  }

  const result = await loginMember({
    email,
    password,
  });

  if (!result.data) {
    redirect(
      `/account/sign-in${buildSearch({
        email,
        signInError: result.message ?? toLocalizedQueryMessage("signInFailedMessage"),
        returnPath,
      })}`,
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
