"use server";

import { redirect } from "next/navigation";
import {
  confirmMemberEmail,
  registerMember,
  requestMemberEmailConfirmation,
  requestMemberPasswordReset,
  resetMemberPassword,
} from "@/features/account/api/member-auth";
import { readNormalizedEmail, readTrimmedFormText } from "@/lib/form-data";
import { buildAppQueryPath, sanitizeAppPath } from "@/lib/locale-routing";
import { toLocalizedQueryMessage } from "@/localization";

function buildAccountFlowPath(
  pathname: string,
  values: Record<string, string | undefined>,
  returnPath: string,
) {
  return buildAppQueryPath(pathname, {
    ...values,
    returnPath,
  });
}

export async function registerMemberAction(formData: FormData) {
  const firstName = readTrimmedFormText(formData, "firstName", 80);
  const lastName = readTrimmedFormText(formData, "lastName", 80);
  const email = readNormalizedEmail(formData);
  const password = readTrimmedFormText(formData, "password", 256);
  const returnPath = sanitizeAppPath(
    readTrimmedFormText(formData, "returnPath", 512) || "/account",
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
      registerStatus: toLocalizedQueryMessage("registrationSubmittedMessage"),
    }, returnPath),
  );
}

export async function requestEmailConfirmationAction(formData: FormData) {
  const email = readNormalizedEmail(formData);
  const returnPath = sanitizeAppPath(
    readTrimmedFormText(formData, "returnPath", 512) || "/account",
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
      activationStatus: toLocalizedQueryMessage("activationRequestedMessage"),
    }, returnPath),
  );
}

export async function confirmEmailAction(formData: FormData) {
  const email = readNormalizedEmail(formData);
  const token = readTrimmedFormText(formData, "token", 256);
  const returnPath = sanitizeAppPath(
    readTrimmedFormText(formData, "returnPath", 512) || "/account",
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
      activationStatus: toLocalizedQueryMessage("activationConfirmedMessage"),
    }, returnPath),
  );
}

export async function requestPasswordResetAction(formData: FormData) {
  const email = readNormalizedEmail(formData);
  const returnPath = sanitizeAppPath(
    readTrimmedFormText(formData, "returnPath", 512) || "/account",
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
      passwordStatus: toLocalizedQueryMessage("passwordRequestedMessage"),
    }, returnPath),
  );
}

export async function resetPasswordAction(formData: FormData) {
  const email = readNormalizedEmail(formData);
  const token = readTrimmedFormText(formData, "token", 256);
  const newPassword = readTrimmedFormText(formData, "newPassword", 256);
  const returnPath = sanitizeAppPath(
    readTrimmedFormText(formData, "returnPath", 512) || "/account",
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
      passwordStatus: toLocalizedQueryMessage("passwordResetMessage"),
    }, returnPath),
  );
}
