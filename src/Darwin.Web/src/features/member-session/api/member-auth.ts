import "server-only";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import type { PublicApiFetchResult } from "@/lib/api/fetch-public-json";
import { toLocalizedQueryMessage } from "@/localization";

type TokenResponse = {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  userId: string;
  email: string;
};

async function postAuthJson<T>(
  path: string,
  body: unknown,
  accessToken?: string,
): Promise<PublicApiFetchResult<T>> {
  const { webApiBaseUrl } = getSiteRuntimeConfig();

  try {
    const response = await fetch(`${webApiBaseUrl}${path}`, {
      method: "POST",
      cache: "no-store",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
        ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      },
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      let detail = toLocalizedQueryMessage("memberAuthHttpErrorMessage");
      try {
        const problem = (await response.json()) as { detail?: string; title?: string };
        detail = problem.detail ?? problem.title ?? detail;
      } catch {
        // Keep status detail.
      }

      return {
        data: null,
        status: response.status === 404 ? "not-found" : "http-error",
        message: detail,
      };
    }

    try {
      return {
        data: (await response.json()) as T,
        status: "ok",
      };
    } catch {
      return {
        data: null,
        status: "ok",
      };
    }
  } catch {
    return {
      data: null,
      status: "network-error",
      message: toLocalizedQueryMessage("memberAuthNetworkErrorMessage"),
    };
  }
}

export async function loginMember(input: {
  email: string;
  password: string;
}) {
  return postAuthJson<TokenResponse>("/api/v1/member/auth/login", {
    email: input.email,
    password: input.password,
  });
}

export async function refreshMember(input: {
  refreshToken: string;
}) {
  return postAuthJson<TokenResponse>("/api/v1/member/auth/refresh", {
    refreshToken: input.refreshToken,
  });
}

export async function logoutMember(input: {
  accessToken: string;
  refreshToken: string;
}) {
  return postAuthJson<never>(
    "/api/v1/member/auth/logout",
    {
      refreshToken: input.refreshToken,
    },
    input.accessToken,
  );
}
