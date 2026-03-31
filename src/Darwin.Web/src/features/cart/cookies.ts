import "server-only";
import { cookies } from "next/headers";
import type { CartDisplaySnapshot } from "@/features/cart/types";

const ANONYMOUS_CART_COOKIE = "darwin-storefront-anonymous-id";
const CART_DISPLAY_COOKIE = "darwin-storefront-cart-display";

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
    const parsed = JSON.parse(raw) as CartDisplaySnapshot[];
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

export async function writeCartDisplaySnapshots(
  snapshots: CartDisplaySnapshot[],
) {
  const cookieStore = await cookies();
  cookieStore.set(
    CART_DISPLAY_COOKIE,
    JSON.stringify(snapshots.slice(0, 50)),
    getCookieBaseOptions(),
  );
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
  await writeCartDisplaySnapshots(next);
}

export async function clearStorefrontCartState() {
  const cookieStore = await cookies();
  cookieStore.delete(ANONYMOUS_CART_COOKIE);
  cookieStore.delete(CART_DISPLAY_COOKIE);
}
