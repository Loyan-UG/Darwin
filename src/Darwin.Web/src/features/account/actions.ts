"use server";

import { redirect } from "next/navigation";
import {
  confirmMemberEmail,
  registerMember,
  requestMemberEmailConfirmation,
  requestMemberPasswordReset,
  resetMemberPassword,
} from "@/features/account/api/member-auth";

function encodeQuery(values: Record<string, string | undefined>) {
  const params = new URLSearchParams();

  for (const [key, value] of Object.entries(values)) {
    if (value) {
      params.set(key, value);
    }
  }

  const query = params.toString();
  return query ? `?${query}` : "";
}

export async function registerMemberAction(formData: FormData) {
  const firstName = String(formData.get("firstName") ?? "").trim();
  const lastName = String(formData.get("lastName") ?? "").trim();
  const email = String(formData.get("email") ?? "").trim();
  const password = String(formData.get("password") ?? "").trim();

  if (!firstName || !lastName || !email || !password) {
    redirect(
      `/account/register${encodeQuery({
        email,
        registerError: "All registration fields are required.",
      })}`,
    );
  }

  const result = await registerMember({
    firstName,
    lastName,
    email,
    password,
  });

  if (result.status !== "ok") {
    redirect(
      `/account/register${encodeQuery({
        email,
        registerError: result.message ?? "Registration could not be completed.",
      })}`,
    );
  }

  redirect(
    `/account/register${encodeQuery({
      email,
      registerStatus: "registered",
    })}`,
  );
}

export async function requestEmailConfirmationAction(formData: FormData) {
  const email = String(formData.get("email") ?? "").trim();

  if (!email) {
    redirect(
      `/account/activation${encodeQuery({
        activationError: "Email is required to request confirmation.",
      })}`,
    );
  }

  const result = await requestMemberEmailConfirmation({
    email,
  });

  if (result.status !== "ok") {
    redirect(
      `/account/activation${encodeQuery({
        email,
        activationError:
          result.message ?? "Activation email could not be requested.",
      })}`,
    );
  }

  redirect(
    `/account/activation${encodeQuery({
      email,
      activationStatus: "requested",
    })}`,
  );
}

export async function confirmEmailAction(formData: FormData) {
  const email = String(formData.get("email") ?? "").trim();
  const token = String(formData.get("token") ?? "").trim();

  if (!email || !token) {
    redirect(
      `/account/activation${encodeQuery({
        email,
        token,
        activationError: "Email and token are both required to confirm the account.",
      })}`,
    );
  }

  const result = await confirmMemberEmail({
    email,
    token,
  });

  if (result.status !== "ok") {
    redirect(
      `/account/activation${encodeQuery({
        email,
        token,
        activationError: result.message ?? "Email confirmation could not be completed.",
      })}`,
    );
  }

  redirect(
    `/account/activation${encodeQuery({
      email,
      activationStatus: "confirmed",
    })}`,
  );
}

export async function requestPasswordResetAction(formData: FormData) {
  const email = String(formData.get("email") ?? "").trim();

  if (!email) {
    redirect(
      `/account/password${encodeQuery({
        passwordError: "Email is required to request a password reset.",
      })}`,
    );
  }

  const result = await requestMemberPasswordReset({
    email,
  });

  if (result.status !== "ok") {
    redirect(
      `/account/password${encodeQuery({
        email,
        passwordError:
          result.message ?? "Password reset request could not be completed.",
      })}`,
    );
  }

  redirect(
    `/account/password${encodeQuery({
      email,
      passwordStatus: "requested",
    })}`,
  );
}

export async function resetPasswordAction(formData: FormData) {
  const email = String(formData.get("email") ?? "").trim();
  const token = String(formData.get("token") ?? "").trim();
  const newPassword = String(formData.get("newPassword") ?? "").trim();

  if (!email || !token || !newPassword) {
    redirect(
      `/account/password${encodeQuery({
        email,
        token,
        passwordError: "Email, token, and new password are required.",
      })}`,
    );
  }

  const result = await resetMemberPassword({
    email,
    token,
    newPassword,
  });

  if (result.status !== "ok") {
    redirect(
      `/account/password${encodeQuery({
        email,
        token,
        passwordError: result.message ?? "Password could not be reset.",
      })}`,
    );
  }

  redirect(
    `/account/password${encodeQuery({
      email,
      passwordStatus: "reset",
    })}`,
  );
}
