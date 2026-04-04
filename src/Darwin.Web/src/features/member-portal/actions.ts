"use server";

import { redirect } from "next/navigation";
import {
  changeCurrentMemberPassword,
  prepareCurrentMemberLoyaltyScanSession,
  confirmCurrentMemberPhoneVerification,
  createCurrentMemberAddress,
  deleteCurrentMemberAddress,
  joinCurrentMemberLoyaltyBusiness,
  trackCurrentMemberPromotionInteraction,
  requestCurrentMemberPhoneVerification,
  setCurrentMemberAddressDefault,
  updateCurrentMemberAddress,
  updateCurrentMemberPreferences,
  updateCurrentMemberProfile,
} from "@/features/member-portal/api/member-portal";
import {
  clearPreparedMemberLoyaltyScanSession,
  writePreparedMemberLoyaltyScanSession,
} from "@/features/member-portal/scan-session-cookie";
import { getFreshMemberAccessToken } from "@/features/member-session/server";
import { readNormalizedEmail, readTrimmedFormText } from "@/lib/form-data";
import {
  appendAppQueryParam,
  buildAppQueryPath,
  sanitizeAppPath,
} from "@/lib/locale-routing";
import { toLocalizedQueryMessage } from "@/localization";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import { buildWebApiFetchInit } from "@/lib/webapi-fetch";

function withFlash(path: string, key: string, value: string) {
  return appendAppQueryParam(path, key, value);
}

function normalizeId(value: FormDataEntryValue | null) {
  return String(value ?? "").trim();
}

async function createPaymentIntent(path: string) {
  const accessToken = await getFreshMemberAccessToken();
  if (!accessToken) {
    return {
      ok: false,
      message: toLocalizedQueryMessage("memberSessionRequiredMessage"),
    };
  }

  const { webApiBaseUrl } = getSiteRuntimeConfig();

  try {
    const requestUrl = `${webApiBaseUrl}${path}`;
    const response = await fetch(requestUrl, buildWebApiFetchInit(requestUrl, {
      method: "POST",
      cache: "no-store",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify({}),
    }));

    if (!response.ok) {
      let detail = toLocalizedQueryMessage("memberPaymentHandoffHttpErrorMessage");
      try {
        const problem = (await response.json()) as { detail?: string; title?: string };
        detail = problem.detail ?? problem.title ?? detail;
      } catch {
        // Keep status detail.
      }

      return {
        ok: false,
        message: detail,
      };
    }

    const payload = (await response.json()) as { checkoutUrl?: string };
    return payload.checkoutUrl
      ? {
          ok: true,
          checkoutUrl: payload.checkoutUrl,
        }
      : {
          ok: false,
          message: toLocalizedQueryMessage("memberPaymentHandoffMissingUrlMessage"),
        };
  } catch {
    return {
      ok: false,
      message: toLocalizedQueryMessage("memberPaymentHandoffUnreachableMessage"),
    };
  }
}

export async function createMemberOrderPaymentIntentAction(formData: FormData) {
  const orderId = String(formData.get("orderId") ?? "").trim();
  const failurePath = sanitizeAppPath(
    String(formData.get("failurePath") ?? `/orders/${orderId}`),
    `/orders/${orderId}`,
  );

  if (!orderId) {
    redirect("/orders");
  }

  const result = await createPaymentIntent(
    `/api/v1/member/orders/${orderId}/payment-intent`,
  );

  if (!result.ok || !result.checkoutUrl) {
    redirect(
      appendAppQueryParam(
        failurePath,
        "paymentError",
        result.message ?? toLocalizedQueryMessage("memberPaymentHandoffFailedMessage"),
      ),
    );
  }

  redirect(result.checkoutUrl);
}

export async function updateMemberProfileAction(formData: FormData) {
  const id = readTrimmedFormText(formData, "id", 128);
  const email = readNormalizedEmail(formData);
  const firstName = readTrimmedFormText(formData, "firstName", 80);
  const lastName = readTrimmedFormText(formData, "lastName", 80);
  const phoneE164 = readTrimmedFormText(formData, "phoneE164", 32);
  const locale = readTrimmedFormText(formData, "locale", 32);
  const timezone = readTrimmedFormText(formData, "timezone", 80);
  const currency = readTrimmedFormText(formData, "currency", 8);
  const rowVersion = readTrimmedFormText(formData, "rowVersion", 256);

  if (!id || !rowVersion || !locale || !timezone || !currency) {
    redirect(
      withFlash(
        "/account/profile",
        "profileError",
        toLocalizedQueryMessage("profileRequiredFieldsMessage"),
      ),
    );
  }

  const result = await updateCurrentMemberProfile({
    id,
    email,
    firstName,
    lastName,
    phoneE164: phoneE164 || null,
    locale,
    timezone,
    currency,
    rowVersion,
  });

  if (result.status !== "ok") {
    redirect(
      withFlash(
        "/account/profile",
        "profileError",
        result.message ?? toLocalizedQueryMessage("profileUpdateFailedMessage"),
      ),
    );
  }

  redirect(withFlash("/account/profile", "profileStatus", "saved"));
}

export async function updateMemberPreferencesAction(formData: FormData) {
  const rowVersion = String(formData.get("rowVersion") ?? "").trim();
  if (!rowVersion) {
    redirect(
      withFlash(
        "/account/preferences",
        "preferencesError",
        toLocalizedQueryMessage("preferencesRowVersionRequiredMessage"),
      ),
    );
  }

  const result = await updateCurrentMemberPreferences({
    rowVersion,
    marketingConsent: formData.get("marketingConsent") === "on",
    allowEmailMarketing: formData.get("allowEmailMarketing") === "on",
    allowSmsMarketing: formData.get("allowSmsMarketing") === "on",
    allowWhatsAppMarketing: formData.get("allowWhatsAppMarketing") === "on",
    allowPromotionalPushNotifications:
      formData.get("allowPromotionalPushNotifications") === "on",
    allowOptionalAnalyticsTracking:
      formData.get("allowOptionalAnalyticsTracking") === "on",
  });

  if (result.status !== "ok") {
    redirect(
      withFlash(
        "/account/preferences",
        "preferencesError",
        result.message ??
          toLocalizedQueryMessage("preferencesUpdateFailedMessage"),
      ),
    );
  }

  redirect(withFlash("/account/preferences", "preferencesStatus", "saved"));
}

export async function changeMemberPasswordAction(formData: FormData) {
  const currentPassword = readTrimmedFormText(formData, "currentPassword", 256);
  const newPassword = readTrimmedFormText(formData, "newPassword", 256);
  const confirmPassword = readTrimmedFormText(formData, "confirmPassword", 256);

  if (!currentPassword || !newPassword || !confirmPassword) {
    redirect(
      withFlash(
        "/account/security",
        "securityError",
        toLocalizedQueryMessage("securityRequiredFieldsMessage"),
      ),
    );
  }

  if (newPassword !== confirmPassword) {
    redirect(
      withFlash(
        "/account/security",
        "securityError",
        toLocalizedQueryMessage("securityConfirmMismatchMessage"),
      ),
    );
  }

  const result = await changeCurrentMemberPassword({
    currentPassword,
    newPassword,
  });

  if (result.status !== "ok") {
    redirect(
      withFlash(
        "/account/security",
        "securityError",
        result.message ?? toLocalizedQueryMessage("securityUpdateFailedMessage"),
      ),
    );
  }

  redirect(withFlash("/account/security", "securityStatus", "saved"));
}

export async function requestMemberPhoneVerificationAction(formData: FormData) {
  const channel = readTrimmedFormText(formData, "channel", 32);
  const result = await requestCurrentMemberPhoneVerification({
    channel: channel || null,
  });

  if (result.status !== "ok") {
    redirect(
      withFlash(
        "/account/profile",
        "phoneError",
        result.message ??
          toLocalizedQueryMessage("phoneVerificationRequestFailedMessage"),
      ),
    );
  }

  redirect(withFlash("/account/profile", "phoneStatus", "requested"));
}

export async function confirmMemberPhoneVerificationAction(formData: FormData) {
  const code = readTrimmedFormText(formData, "code", 32);
  if (!code) {
    redirect(
      withFlash(
        "/account/profile",
        "phoneError",
        toLocalizedQueryMessage("phoneVerificationCodeRequiredMessage"),
      ),
    );
  }

  const result = await confirmCurrentMemberPhoneVerification({
    code,
  });

  if (result.status !== "ok") {
    redirect(
      withFlash(
        "/account/profile",
        "phoneError",
        result.message ??
          toLocalizedQueryMessage("phoneVerificationConfirmFailedMessage"),
      ),
    );
  }

  redirect(withFlash("/account/profile", "phoneStatus", "confirmed"));
}

export async function createMemberAddressAction(formData: FormData) {
  const result = await createCurrentMemberAddress({
    fullName: String(formData.get("fullName") ?? "").trim(),
    company: String(formData.get("company") ?? "").trim() || null,
    street1: String(formData.get("street1") ?? "").trim(),
    street2: String(formData.get("street2") ?? "").trim() || null,
    postalCode: String(formData.get("postalCode") ?? "").trim(),
    city: String(formData.get("city") ?? "").trim(),
    state: String(formData.get("state") ?? "").trim() || null,
    countryCode: String(formData.get("countryCode") ?? "").trim() || "DE",
    phoneE164: String(formData.get("phoneE164") ?? "").trim() || null,
    isDefaultBilling: formData.get("isDefaultBilling") === "on",
    isDefaultShipping: formData.get("isDefaultShipping") === "on",
  });

  if (result.status !== "ok") {
    redirect(
      withFlash(
        "/account/addresses",
        "addressesError",
        result.message ?? toLocalizedQueryMessage("addressCreateFailedMessage"),
      ),
    );
  }

  redirect(withFlash("/account/addresses", "addressesStatus", "created"));
}

export async function updateMemberAddressAction(formData: FormData) {
  const id = String(formData.get("id") ?? "").trim();
  const rowVersion = String(formData.get("rowVersion") ?? "").trim();

  if (!id || !rowVersion) {
    redirect(
      withFlash(
        "/account/addresses",
        "addressesError",
        toLocalizedQueryMessage("addressUpdateIdentifiersMessage"),
      ),
    );
  }

  const result = await updateCurrentMemberAddress(id, {
    rowVersion,
    fullName: String(formData.get("fullName") ?? "").trim(),
    company: String(formData.get("company") ?? "").trim() || null,
    street1: String(formData.get("street1") ?? "").trim(),
    street2: String(formData.get("street2") ?? "").trim() || null,
    postalCode: String(formData.get("postalCode") ?? "").trim(),
    city: String(formData.get("city") ?? "").trim(),
    state: String(formData.get("state") ?? "").trim() || null,
    countryCode: String(formData.get("countryCode") ?? "").trim() || "DE",
    phoneE164: String(formData.get("phoneE164") ?? "").trim() || null,
    isDefaultBilling: formData.get("isDefaultBilling") === "on",
    isDefaultShipping: formData.get("isDefaultShipping") === "on",
  });

  if (result.status !== "ok") {
    redirect(
      withFlash(
        "/account/addresses",
        "addressesError",
        result.message ?? toLocalizedQueryMessage("addressUpdateFailedMessage"),
      ),
    );
  }

  redirect(withFlash("/account/addresses", "addressesStatus", "updated"));
}

export async function deleteMemberAddressAction(formData: FormData) {
  const id = String(formData.get("id") ?? "").trim();
  const rowVersion = String(formData.get("rowVersion") ?? "").trim();

  if (!id || !rowVersion) {
    redirect(
      withFlash(
        "/account/addresses",
        "addressesError",
        toLocalizedQueryMessage("addressDeleteIdentifiersMessage"),
      ),
    );
  }

  const result = await deleteCurrentMemberAddress(id, rowVersion);
  if (result.status !== "ok") {
    redirect(
      withFlash(
        "/account/addresses",
        "addressesError",
        result.message ?? toLocalizedQueryMessage("addressDeleteFailedMessage"),
      ),
    );
  }

  redirect(withFlash("/account/addresses", "addressesStatus", "deleted"));
}

export async function setMemberAddressDefaultAction(formData: FormData) {
  const id = String(formData.get("id") ?? "").trim();
  const asBilling = String(formData.get("asBilling") ?? "") === "true";
  const asShipping = String(formData.get("asShipping") ?? "") === "true";

  if (!id || (!asBilling && !asShipping)) {
    redirect(
      withFlash(
        "/account/addresses",
        "addressesError",
        toLocalizedQueryMessage("addressDefaultIncompleteMessage"),
      ),
    );
  }

  const result = await setCurrentMemberAddressDefault(id, {
    asBilling,
    asShipping,
  });

  if (result.status !== "ok") {
    redirect(
      withFlash(
        "/account/addresses",
        "addressesError",
        result.message ?? toLocalizedQueryMessage("addressDefaultFailedMessage"),
      ),
    );
  }

  redirect(withFlash("/account/addresses", "addressesStatus", "default-updated"));
}

export async function trackMemberPromotionInteractionAction(formData: FormData) {
  const businessId = normalizeId(formData.get("businessId"));
  const businessName = normalizeId(formData.get("businessName"));
  const title = normalizeId(formData.get("title"));
  const ctaKind = normalizeId(formData.get("ctaKind"));
  const eventType = String(formData.get("eventType") ?? "Open").trim();
  const returnPath = sanitizeAppPath(
    String(formData.get("returnPath") ?? "/loyalty"),
    "/loyalty",
  );

  if (!businessId || !businessName || !title || !ctaKind) {
    redirect(
      withFlash(
        returnPath,
        "promotionError",
        toLocalizedQueryMessage("promotionTrackingIncompleteMessage"),
      ),
    );
  }

  const normalizedEventType =
    eventType === "Claim" || eventType === "Impression" ? eventType : "Open";

  const result = await trackCurrentMemberPromotionInteraction({
    businessId,
    businessName,
    title,
    ctaKind,
    eventType: normalizedEventType,
  });

  if (result.status !== "ok") {
    redirect(
      withFlash(
        returnPath,
        "promotionError",
        result.message ??
          toLocalizedQueryMessage("promotionTrackingFailedMessage"),
      ),
    );
  }

  redirect(withFlash(returnPath, "promotionStatus", "tracked"));
}

export async function joinMemberLoyaltyBusinessAction(formData: FormData) {
  const businessId = normalizeId(formData.get("businessId"));
  const businessLocationId = normalizeId(formData.get("businessLocationId"));
  const returnPath = sanitizeAppPath(
    String(formData.get("returnPath") ?? `/loyalty/${businessId}`),
    `/loyalty/${businessId}`,
  );

  if (!businessId) {
    redirect("/loyalty");
  }

  const result = await joinCurrentMemberLoyaltyBusiness({
    businessId,
    businessLocationId: businessLocationId || null,
  });

  if (result.status === "unauthenticated" || result.status === "unauthorized") {
    redirect(buildAppQueryPath("/account/sign-in", { returnPath }));
  }

  if (result.status !== "ok") {
    redirect(
      withFlash(
        returnPath,
        "joinError",
        result.message ?? toLocalizedQueryMessage("loyaltyJoinFailedMessage"),
      ),
    );
  }

  redirect(withFlash(returnPath, "joinStatus", "joined"));
}

export async function prepareMemberLoyaltyScanSessionAction(formData: FormData) {
  const businessId = normalizeId(formData.get("businessId"));
  const businessLocationId = normalizeId(formData.get("businessLocationId"));
  const mode = String(formData.get("mode") ?? "Accrual").trim();
  const returnPath = sanitizeAppPath(
    String(formData.get("returnPath") ?? `/loyalty/${businessId}`),
    `/loyalty/${businessId}`,
  );
  const selectedRewardTierIds = formData
    .getAll("selectedRewardTierIds")
    .map((value) => normalizeId(value))
    .filter(Boolean)
    .filter((value, index, items) => items.indexOf(value) === index);

  if (!businessId) {
    redirect("/loyalty");
  }

  if (mode === "Redemption" && selectedRewardTierIds.length === 0) {
    redirect(
      withFlash(
        returnPath,
        "scanError",
        toLocalizedQueryMessage("scanRewardSelectionRequiredMessage"),
      ),
    );
  }

  const normalizedMode = mode === "Redemption" ? "Redemption" : "Accrual";
  const result = await prepareCurrentMemberLoyaltyScanSession({
    businessId,
    businessLocationId: businessLocationId || null,
    mode: normalizedMode,
    selectedRewardTierIds,
    deviceId: "Darwin.Web",
  });

  if (result.status === "unauthenticated" || result.status === "unauthorized") {
    redirect(buildAppQueryPath("/account/sign-in", { returnPath }));
  }

  if (result.status !== "ok" || !result.data) {
    redirect(
      withFlash(
        returnPath,
        "scanError",
        result.message ?? toLocalizedQueryMessage("scanPrepareFailedMessage"),
      ),
    );
  }

  await writePreparedMemberLoyaltyScanSession({
    ...result.data,
    businessId,
  });

  redirect(withFlash(returnPath, "scanStatus", "prepared"));
}

export async function clearMemberLoyaltyScanSessionAction(formData: FormData) {
  const businessId = normalizeId(formData.get("businessId"));
  const returnPath = sanitizeAppPath(
    String(formData.get("returnPath") ?? `/loyalty/${businessId}`),
    `/loyalty/${businessId}`,
  );

  if (!businessId) {
    redirect("/loyalty");
  }

  await clearPreparedMemberLoyaltyScanSession();
  redirect(withFlash(returnPath, "scanStatus", "cleared"));
}

export async function createMemberInvoicePaymentIntentAction(formData: FormData) {
  const invoiceId = String(formData.get("invoiceId") ?? "").trim();
  const failurePath = sanitizeAppPath(
    String(formData.get("failurePath") ?? `/invoices/${invoiceId}`),
    `/invoices/${invoiceId}`,
  );

  if (!invoiceId) {
    redirect("/invoices");
  }

  const result = await createPaymentIntent(
    `/api/v1/member/invoices/${invoiceId}/payment-intent`,
  );

  if (!result.ok || !result.checkoutUrl) {
    redirect(
      appendAppQueryParam(
        failurePath,
        "paymentError",
        result.message ?? toLocalizedQueryMessage("memberPaymentHandoffFailedMessage"),
      ),
    );
  }

  redirect(result.checkoutUrl);
}
