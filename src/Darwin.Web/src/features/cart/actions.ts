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
  removePublicCartItem,
  updatePublicCartItem,
} from "@/features/cart/api/public-cart";

function withCartFlash(path: string, key: string, value: string) {
  const separator = path.includes("?") ? "&" : "?";
  return `${path}${separator}${key}=${encodeURIComponent(value)}`;
}

function revalidateStorefrontPaths(paths: string[]) {
  for (const path of paths) {
    revalidatePath(path);
  }
}

export async function addToCartAction(formData: FormData) {
  const variantId = String(formData.get("variantId") ?? "").trim();
  const returnPath = String(formData.get("returnPath") ?? "/catalog").trim();
  const quantity = Number(formData.get("quantity") ?? "1");
  const productName = String(formData.get("productName") ?? "").trim();
  const productHref = String(formData.get("productHref") ?? "").trim();
  const productImageUrl = String(formData.get("productImageUrl") ?? "").trim();
  const productImageAlt = String(formData.get("productImageAlt") ?? "").trim();
  const productSku = String(formData.get("productSku") ?? "").trim();

  if (!variantId || !Number.isFinite(quantity) || quantity <= 0) {
    redirect(withCartFlash(returnPath, "cartError", "Invalid cart request."));
  }

  const anonymousId = await getOrCreateAnonymousCartId();
  const result = await addItemToPublicCart({
    anonymousId,
    variantId,
    quantity,
  });

  if (!result.data) {
    redirect(
      withCartFlash(
        returnPath,
        "cartError",
        result.message ?? "Cart item could not be added.",
      ),
    );
  }

  await upsertCartDisplaySnapshot({
    variantId,
    name: productName || "Storefront item",
    href: productHref || returnPath,
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
  const quantity = Number(formData.get("quantity") ?? "1");
  const selectedAddOnValueIdsJson = String(
    formData.get("selectedAddOnValueIdsJson") ?? "",
  ).trim();

  if (!cartId || !variantId || !Number.isFinite(quantity) || quantity < 0) {
    redirect(withCartFlash("/cart", "cartError", "Invalid cart update request."));
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
        result.message ?? "Cart quantity could not be updated.",
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
    redirect(withCartFlash("/cart", "cartError", "Invalid cart remove request."));
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
        result.message ?? "Cart item could not be removed.",
      ),
    );
  }

  await pruneCartDisplaySnapshots(result.data.items.map((item) => item.variantId));
  revalidateStorefrontPaths(["/cart"]);
  redirect(withCartFlash("/cart", "cartStatus", "removed"));
}
