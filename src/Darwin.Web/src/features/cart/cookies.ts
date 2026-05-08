import "server-only";
import { cookies } from "next/headers";
import type { CartDisplaySnapshot } from "@/features/cart/types";
import { sanitizeAppPath } from "@/lib/locale-routing";

const ANONYMOUS_CART_COOKIE = "darwin-storefront-anonymous-id";
const CART_DISPLAY_COOKIE = "darwin-storefront-cart-display";

function isCartDisplaySnapshot(value: unknown): value is CartDisplaySnapshot {
  if (!value || typeof value !== "object") {
    return false;
  }

  const snapshot = value as Record<string, unknown>;
  return (
    typeof snapshot.variantId === "string" &&
    snapshot.variantId.trim().length > 0 &&
    typeof snapshot.name === "string" &&
    snapshot.name.trim().length > 0 &&
    typeof snapshot.href === "string" &&
    sanitizeAppPath(snapshot.href, "/catalog").startsWith("/")
  );
}

function getCookieBaseOptions() {
  return {
    httpOnly: true,
    sameSite: "lax" as const,
    path: "/",
    secure: process.env.NODE_ENV === "production",
    maxAge: 60 * 60 * 24 * 30,
  };
}

export async function getOrCreateAnonymousCartId() {
  const cookieStore = await cookies();
  const existing = cookieStore.get(ANONYMOUS_CART_COOKIE)?.value;
  if (existing) {
    return existing;
  }

  const created = crypto.randomUUID();
  cookieStore.set(ANONYMOUS_CART_COOKIE, created, getCookieBaseOptions());
  return created;
}

export async function getAnonymousCartId() {
  const cookieStore = await cookies();
  return cookieStore.get(ANONYMOUS_CART_COOKIE)?.value ?? null;
}

export async function readCartDisplaySnapshots() {
  const cookieStore = await cookies();
  const raw = cookieStore.get(CART_DISPLAY_COOKIE)?.value;
  if (!raw) {
    return [] satisfies CartDisplaySnapshot[];
  }

  try {
    const parsed = JSON.parse(raw) as unknown;
    return Array.isArray(parsed) ? parsed.filter(isCartDisplaySnapshot) : [];
  } catch {
    return [];
  }
}

export async function writeCartDisplaySnapshots(
  snapshots: CartDisplaySnapshot[],
) {
  const cookieStore = await cookies();
  try {
    cookieStore.set(
      CART_DISPLAY_COOKIE,
      JSON.stringify(snapshots.slice(0, 50)),
      getCookieBaseOptions(),
    );
  } catch (error) {
    if (
      error instanceof Error &&
      error.message.includes("Cookies can only be modified")
    ) {
      return;
    }

    throw error;
  }
}

export async function upsertCartDisplaySnapshot(snapshot: CartDisplaySnapshot) {
  const current = await readCartDisplaySnapshots();
  const next = [
    snapshot,
    ...current.filter((item) => item.variantId !== snapshot.variantId),
  ];
  await writeCartDisplaySnapshots(next);
}

export async function pruneCartDisplaySnapshots(activeVariantIds: string[]) {
  const current = await readCartDisplaySnapshots();
  const next = current.filter((item) => activeVariantIds.includes(item.variantId));
  if (next.length === current.length) {
    return;
  }

  await writeCartDisplaySnapshots(next);
}

export async function clearStorefrontCartState() {
  const cookieStore = await cookies();
  cookieStore.delete(ANONYMOUS_CART_COOKIE);
  cookieStore.delete(CART_DISPLAY_COOKIE);
}
