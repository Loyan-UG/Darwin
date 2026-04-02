"use server";

import { redirect } from "next/navigation";
import {
  confirmMemberEmail,
  registerMember,
  requestMemberEmailConfirmation,
  requestMemberPasswordReset,
  resetMemberPassword,
} from "@/features/account/api/member-auth";
import { sanitizeAppPath } from "@/lib/locale-routing";
import { toLocalizedQueryMessage } from "@/localization";

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

function buildAccountFlowPath(
  pathname: string,
  values: Record<string, string | undefined>,
  returnPath: string,
) {
  return `${pathname}${encodeQuery({
    ...values,
    returnPath,
  })}`;
}

export async function registerMemberAction(formData: FormData) {
  const firstName = String(formData.get("firstName") ?? "").trim();
  const lastName = String(formData.get("lastName") ?? "").trim();
  const email = String(formData.get("email") ?? "").trim();
  const password = String(formData.get("password") ?? "").trim();
  const returnPath = sanitizeAppPath(
    String(formData.get("returnPath") ?? "/account"),
    "/account",
  );

  if (!firstName || !lastName || !email || !password) {
    redirect(
      buildAccountFlowPath("/account/register", {
        email,
        registerError: toLocalizedQueryMessage("registrationFieldsRequiredMessage"),
      }, returnPath),
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
      buildAccountFlowPath("/account/register", {
        email,
        registerError:
          result.message ?? toLocalizedQueryMessage("registrationFailedMessage"),
      }, returnPath),
    );
  }

  redirect(
    buildAccountFlowPath("/account/register", {
      email,
      registerStatus: "registered",
    }, returnPath),
  );
}

export async function requestEmailConfirmationAction(formData: FormData) {
  const email = String(formData.get("email") ?? "").trim();
  const returnPath = sanitizeAppPath(
    String(formData.get("returnPath") ?? "/account"),
    "/account",
  );

  if (!email) {
    redirect(
      buildAccountFlowPath("/account/activation", {
        activationError: toLocalizedQueryMessage("activationEmailRequiredMessage"),
      }, returnPath),
    );
  }

  const result = await requestMemberEmailConfirmation({
    email,
  });

  if (result.status !== "ok") {
    redirect(
      buildAccountFlowPath("/account/activation", {
        email,
        activationError:
          result.message ??
          toLocalizedQueryMessage("activationRequestFailedMessage"),
      }, returnPath),
    );
  }

  redirect(
    buildAccountFlowPath("/account/activation", {
      email,
      activationStatus: "requested",
    }, returnPath),
  );
}

export async function confirmEmailAction(formData: FormData) {
  const email = String(formData.get("email") ?? "").trim();
  const token = String(formData.get("token") ?? "").trim();
  const returnPath = sanitizeAppPath(
    String(formData.get("returnPath") ?? "/account"),
    "/account",
  );

  if (!email || !token) {
    redirect(
      buildAccountFlowPath("/account/activation", {
        email,
        token,
        activationError: toLocalizedQueryMessage(
          "activationEmailTokenRequiredMessage",
        ),
      }, returnPath),
    );
  }

  const result = await confirmMemberEmail({
    email,
    token,
  });

  if (result.status !== "ok") {
    redirect(
      buildAccountFlowPath("/account/activation", {
        email,
        token,
        activationError:
          result.message ??
          toLocalizedQueryMessage("activationConfirmFailedMessage"),
      }, returnPath),
    );
  }

  redirect(
    buildAccountFlowPath("/account/activation", {
      email,
      activationStatus: "confirmed",
    }, returnPath),
  );
}

export async function requestPasswordResetAction(formData: FormData) {
  const email = String(formData.get("email") ?? "").trim();
  const returnPath = sanitizeAppPath(
    String(formData.get("returnPath") ?? "/account"),
    "/account",
  );

  if (!email) {
    redirect(
      buildAccountFlowPath("/account/password", {
        passwordError: toLocalizedQueryMessage(
          "passwordRequestEmailRequiredMessage",
        ),
      }, returnPath),
    );
  }

  const result = await requestMemberPasswordReset({
    email,
  });

  if (result.status !== "ok") {
    redirect(
      buildAccountFlowPath("/account/password", {
        email,
        passwordError:
          result.message ??
          toLocalizedQueryMessage("passwordRequestFailedMessage"),
      }, returnPath),
    );
  }

  redirect(
    buildAccountFlowPath("/account/password", {
      email,
      passwordStatus: "requested",
    }, returnPath),
  );
}

export async function resetPasswordAction(formData: FormData) {
  const email = String(formData.get("email") ?? "").trim();
  const token = String(formData.get("token") ?? "").trim();
  const newPassword = String(formData.get("newPassword") ?? "").trim();
  const returnPath = sanitizeAppPath(
    String(formData.get("returnPath") ?? "/account"),
    "/account",
  );

  if (!email || !token || !newPassword) {
    redirect(
      buildAccountFlowPath("/account/password", {
        email,
        token,
        passwordError: toLocalizedQueryMessage("passwordResetFieldsRequiredMessage"),
      }, returnPath),
    );
  }

  const result = await resetMemberPassword({
    email,
    token,
    newPassword,
  });

  if (result.status !== "ok") {
    redirect(
      buildAccountFlowPath("/account/password", {
        email,
        token,
        passwordError:
          result.message ?? toLocalizedQueryMessage("passwordResetFailedMessage"),
      }, returnPath),
    );
  }

  redirect(
    buildAccountFlowPath("/account/password", {
      email,
      passwordStatus: "reset",
    }, returnPath),
  );
}
