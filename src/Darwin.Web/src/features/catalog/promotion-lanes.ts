import type { PublicProductSummary } from "@/features/catalog/types";
import {
  getCatalogSavingsPercent,
  isOfferProduct,
} from "@/features/catalog/discovery";

export type CatalogPromotionLane =
  | "hero-offers"
  | "value-offers"
  | "live-offers"
  | "base-assortment";

export type CatalogPromotionLaneSummary = {
  lane: CatalogPromotionLane;
  count: number;
  anchorProduct: PublicProductSummary | null;
};

export function summarizeCatalogPromotionLanes(
  products: PublicProductSummary[],
): CatalogPromotionLaneSummary[] {
  const heroOffers = products.filter(
    (product) => getCatalogSavingsPercent(product) >= 25,
  );
  const valueOffers = products.filter(
    (product) => getCatalogSavingsPercent(product) >= 10,
  );
  const liveOffers = products.filter((product) => isOfferProduct(product));
  const baseAssortment = products.filter((product) => !isOfferProduct(product));

  return [
    {
      lane: "hero-offers",
      count: heroOffers.length,
      anchorProduct: heroOffers[0] ?? null,
    },
    {
      lane: "value-offers",
      count: valueOffers.length,
      anchorProduct: valueOffers[0] ?? null,
    },
    {
      lane: "live-offers",
      count: liveOffers.length,
      anchorProduct: liveOffers[0] ?? null,
    },
    {
      lane: "base-assortment",
      count: baseAssortment.length,
      anchorProduct: baseAssortment[0] ?? null,
    },
  ];
}
