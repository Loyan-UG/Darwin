import "server-only";
import type { PublicApiFetchResult } from "@/lib/api/fetch-public-json";
import {
  createDiagnostics,
  getResponseDiagnostics,
  logApiFailure,
} from "@/lib/api-diagnostics";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import type { MemberRegisterResponse } from "@/features/account/types";
import { toLocalizedQueryMessage } from "@/localization";

async function postMemberAuthJson<T>(
  path: string,
  body: unknown,
): Promise<PublicApiFetchResult<T>> {
  const { webApiBaseUrl } = getSiteRuntimeConfig();

  try {
    const response = await fetch(`${webApiBaseUrl}${path}`, {
      method: "POST",
      cache: "no-store",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
    });

    const diagnostics = getResponseDiagnostics("member-auth", path, response);

    if (!response.ok) {
      let detail = toLocalizedQueryMessage("memberAuthHttpErrorMessage");
      try {
        const problem = (await response.json()) as { detail?: string; title?: string };
        detail = problem.detail ?? problem.title ?? detail;
      } catch {
        // Keep the status-based detail.
      }

      logApiFailure(diagnostics, detail);
      return {
        data: null,
        status: "http-error",
        message: detail,
        diagnostics,
      };
    }

    const contentLength = response.headers.get("content-length");
    if (contentLength === "0" || response.status === 204) {
      return {
        data: null,
        status: "ok",
        diagnostics,
      };
    }

    try {
      return {
        data: (await response.json()) as T,
        status: "ok",
        diagnostics,
      };
    } catch (error) {
      logApiFailure(diagnostics, error);
      return {
        data: null,
        status: "ok",
        diagnostics,
      };
    }
  } catch (error) {
    const diagnostics = createDiagnostics("member-auth", path);
    logApiFailure(diagnostics, error);
    return {
      data: null,
      status: "network-error",
      message: toLocalizedQueryMessage("memberAuthNetworkErrorMessage"),
      diagnostics,
    };
  }
}

export async function registerMember(input: {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}) {
  return postMemberAuthJson<MemberRegisterResponse>(
    "/api/v1/member/auth/register",
    input,
  );
}

export async function requestMemberEmailConfirmation(input: {
  email: string;
}) {
  return postMemberAuthJson<never>(
    "/api/v1/member/auth/email/request-confirmation",
    input,
  );
}

export async function confirmMemberEmail(input: {
  email: string;
  token: string;
}) {
  return postMemberAuthJson<never>("/api/v1/member/auth/email/confirm", input);
}

export async function requestMemberPasswordReset(input: {
  email: string;
}) {
  return postMemberAuthJson<never>(
    "/api/v1/member/auth/password/request-reset",
    input,
  );
}

export async function resetMemberPassword(input: {
  email: string;
  token: string;
  newPassword: string;
}) {
  return postMemberAuthJson<never>("/api/v1/member/auth/password/reset", input);
}
