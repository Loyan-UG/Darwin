import type { PublicProductSummary } from "@/features/catalog/types";

export type ProductOpportunityCampaign =
  | "hero-offer"
  | "value-offer"
  | "price-drop"
  | "steady-pick";

type ProductOpportunityCampaignLabels = {
  heroOffer: string;
  valueOffer: string;
  priceDrop: string;
  steadyPick: string;
};

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

export function getProductOpportunityCampaign(
  product: PublicProductSummary,
): ProductOpportunityCampaign {
  const savingsPercent = getProductSavingsPercent(product);

  if (savingsPercent === null) {
    return "steady-pick";
  }

  if (savingsPercent >= 30) {
    return "hero-offer";
  }

  if (savingsPercent >= 15) {
    return "value-offer";
  }

  return "price-drop";
}

export function getProductOpportunityCampaignLabel(
  campaign: ProductOpportunityCampaign,
  labels: ProductOpportunityCampaignLabels,
): string {
  switch (campaign) {
    case "hero-offer":
      return labels.heroOffer;
    case "value-offer":
      return labels.valueOffer;
    case "price-drop":
      return labels.priceDrop;
    default:
      return labels.steadyPick;
  }
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
