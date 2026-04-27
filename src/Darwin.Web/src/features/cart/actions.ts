"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import {
  getOrCreateAnonymousCartId,
  pruneCartDisplaySnapshots,
  upsertCartDisplaySnapshot,
} from "@/features/cart/cookies";
import {
  addItemToPublicCart,
  applyPublicCartCoupon,
  removePublicCartItem,
  updatePublicCartItem,
} from "@/features/cart/api/public-cart";
import {
  normalizeCouponCode,
  readQuantityFromFormData,
} from "@/features/checkout/helpers";
import { appendAppQueryParam, sanitizeAppPath } from "@/lib/locale-routing";
import { toLocalizedQueryMessage } from "@/localization";

function withCartFlash(path: string, key: string, value: string) {
  return appendAppQueryParam(path, key, value);
}

function revalidateStorefrontPaths(paths: string[]) {
  for (const path of paths) {
    revalidatePath(path);
  }
}

export async function addToCartAction(formData: FormData) {
  const variantId = String(formData.get("variantId") ?? "").trim();
  const returnPath = sanitizeAppPath(
    String(formData.get("returnPath") ?? "/catalog"),
    "/catalog",
  );
  const quantity = readQuantityFromFormData(formData, "quantity");
  const productName = String(formData.get("productName") ?? "").trim();
  const productHref = sanitizeAppPath(
    String(formData.get("productHref") ?? ""),
    returnPath,
  );
  const productImageUrl = String(formData.get("productImageUrl") ?? "").trim();
  const productImageAlt = String(formData.get("productImageAlt") ?? "").trim();
  const productSku = String(formData.get("productSku") ?? "").trim();
  const selectedAddOnValueIds = Array.from(formData.entries())
    .filter(([key]) =>
      key === "selectedAddOnValueIds" ||
      key.startsWith("selectedAddOnValueIds:"))
    .map(([, value]) => String(value).trim())
    .filter(Boolean);

  if (!variantId || !Number.isFinite(quantity) || quantity <= 0) {
    redirect(
      withCartFlash(
        returnPath,
        "cartError",
        toLocalizedQueryMessage("cartInvalidRequestMessage"),
      ),
    );
  }

  const anonymousId = await getOrCreateAnonymousCartId();
  const result = await addItemToPublicCart({
    anonymousId,
    variantId,
    quantity,
    selectedAddOnValueIds,
  });

  if (!result.data) {
    redirect(
      withCartFlash(
        returnPath,
        "cartError",
        result.message ?? toLocalizedQueryMessage("cartAddFailedMessage"),
      ),
    );
  }

  await upsertCartDisplaySnapshot({
    variantId,
    name: productName,
    href: productHref,
    imageUrl: productImageUrl || null,
    imageAlt: productImageAlt || null,
    sku: productSku || null,
  });

  await pruneCartDisplaySnapshots(result.data.items.map((item) => item.variantId));
  revalidateStorefrontPaths(["/cart", "/catalog", returnPath]);
  redirect(withCartFlash("/cart", "cartStatus", "added"));
}

export async function updateCartQuantityAction(formData: FormData) {
  const cartId = String(formData.get("cartId") ?? "").trim();
  const variantId = String(formData.get("variantId") ?? "").trim();
  const quantity = readQuantityFromFormData(formData, "quantity");
  const selectedAddOnValueIdsJson = String(
    formData.get("selectedAddOnValueIdsJson") ?? "",
  ).trim();

  if (!cartId || !variantId || !Number.isFinite(quantity) || quantity < 0) {
    redirect(
      withCartFlash(
        "/cart",
        "cartError",
        toLocalizedQueryMessage("cartUpdateInvalidMessage"),
      ),
    );
  }

  const result = await updatePublicCartItem({
    cartId,
    variantId,
    quantity,
    selectedAddOnValueIdsJson: selectedAddOnValueIdsJson || undefined,
  });

  if (!result.data) {
    redirect(
      withCartFlash(
        "/cart",
        "cartError",
        result.message ?? toLocalizedQueryMessage("cartUpdateFailedMessage"),
      ),
    );
  }

  await pruneCartDisplaySnapshots(result.data.items.map((item) => item.variantId));
  revalidateStorefrontPaths(["/cart"]);
  redirect(withCartFlash("/cart", "cartStatus", "updated"));
}

export async function removeCartItemAction(formData: FormData) {
  const cartId = String(formData.get("cartId") ?? "").trim();
  const variantId = String(formData.get("variantId") ?? "").trim();
  const selectedAddOnValueIdsJson = String(
    formData.get("selectedAddOnValueIdsJson") ?? "",
  ).trim();

  if (!cartId || !variantId) {
    redirect(
      withCartFlash(
        "/cart",
        "cartError",
        toLocalizedQueryMessage("cartRemoveInvalidMessage"),
      ),
    );
  }

  const result = await removePublicCartItem({
    cartId,
    variantId,
    selectedAddOnValueIdsJson: selectedAddOnValueIdsJson || undefined,
  });

  if (!result.data) {
    redirect(
      withCartFlash(
        "/cart",
        "cartError",
        result.message ?? toLocalizedQueryMessage("cartRemoveFailedMessage"),
      ),
    );
  }

  await pruneCartDisplaySnapshots(result.data.items.map((item) => item.variantId));
  revalidateStorefrontPaths(["/cart"]);
  redirect(withCartFlash("/cart", "cartStatus", "removed"));
}

export async function applyCartCouponAction(formData: FormData) {
  const cartId = String(formData.get("cartId") ?? "").trim();
  const couponCode = normalizeCouponCode(formData.get("couponCode"));

  if (!cartId) {
    redirect(
      withCartFlash(
        "/cart",
        "cartError",
        toLocalizedQueryMessage("cartCouponMissingCartIdMessage"),
      ),
    );
  }

  const result = await applyPublicCartCoupon({
    cartId,
    couponCode: couponCode || undefined,
  });

  if (!result.data) {
    redirect(
      withCartFlash(
        "/cart",
        "cartError",
        result.message ?? toLocalizedQueryMessage("cartCouponApplyFailedMessage"),
      ),
    );
  }

  await pruneCartDisplaySnapshots(result.data.items.map((item) => item.variantId));
  revalidateStorefrontPaths(["/cart", "/checkout"]);
  redirect(
    withCartFlash(
      "/cart",
      "cartStatus",
      couponCode ? "coupon-applied" : "coupon-cleared",
    ),
  );
}
