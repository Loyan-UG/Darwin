import type { CartDisplaySnapshot } from "@/features/cart/types";
import { sanitizeAppPath } from "@/lib/locale-routing";

export function extractCartLinkedProductSlugs(
  snapshots: CartDisplaySnapshot[],
) {
  const seen = new Set<string>();
  const linkedSlugs: string[] = [];

  for (const snapshot of snapshots) {
    const sanitizedHref = sanitizeAppPath(snapshot.href, "/catalog");
    const match = sanitizedHref.match(/\/catalog\/([^/?#]+)/i);
    const slug = match?.[1];

    if (!slug || seen.has(slug)) {
      continue;
    }

    seen.add(slug);
    linkedSlugs.push(slug);
  }

  return linkedSlugs;
}
