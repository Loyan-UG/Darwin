import type { CartDisplaySnapshot } from "@/features/cart/types";

export function extractCartLinkedProductSlugs(
  snapshots: CartDisplaySnapshot[],
) {
  const seen = new Set<string>();
  const linkedSlugs: string[] = [];

  for (const snapshot of snapshots) {
    const match = snapshot.href.match(/\/catalog\/([^/?#]+)/i);
    const slug = match?.[1];

    if (!slug || seen.has(slug)) {
      continue;
    }

    seen.add(slug);
    linkedSlugs.push(slug);
  }

  return linkedSlugs;
}
