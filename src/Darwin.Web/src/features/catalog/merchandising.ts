import type { PublicProductSummary } from "@/features/catalog/types";

export function getProductSavingsPercent(
  product: PublicProductSummary,
): number | null {
  if (
    typeof product.compareAtPriceMinor !== "number" ||
    product.compareAtPriceMinor <= product.priceMinor
  ) {
    return null;
  }

  return Math.round(
    ((product.compareAtPriceMinor - product.priceMinor) /
      product.compareAtPriceMinor) *
      100,
  );
}

export function sortProductsByOpportunity(
  products: PublicProductSummary[],
): PublicProductSummary[] {
  return [...products].sort((left, right) => {
    const leftSavings = getProductSavingsPercent(left) ?? -1;
    const rightSavings = getProductSavingsPercent(right) ?? -1;

    if (rightSavings !== leftSavings) {
      return rightSavings - leftSavings;
    }

    return left.priceMinor - right.priceMinor;
  });
}

export function getStrongestProductOpportunity(
  products: PublicProductSummary[],
): PublicProductSummary | null {
  return sortProductsByOpportunity(products)[0] ?? null;
}
