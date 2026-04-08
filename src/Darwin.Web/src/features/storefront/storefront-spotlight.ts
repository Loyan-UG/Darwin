import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import {
  getStrongestProductOpportunity,
  sortProductsByOpportunity,
} from "@/features/catalog/merchandising";
import type { PublicPageSummary } from "@/features/cms/types";

type StorefrontSpotlightInput = {
  cmsPages: PublicPageSummary[];
  categories: PublicCategorySummary[];
  products: PublicProductSummary[];
  categoryCampaignCount?: number;
  productCampaignCount?: number;
  offerBoardCount?: number;
};

export function buildStorefrontSpotlightSelections(
  input: StorefrontSpotlightInput,
) {
  const rankedProducts = sortProductsByOpportunity(input.products);
  const categoryCampaignCount = input.categoryCampaignCount ?? 2;
  const productCampaignCount = input.productCampaignCount ?? 2;
  const offerBoardCount = input.offerBoardCount ?? 3;

  return {
    spotlightPage: input.cmsPages[0] ?? null,
    spotlightCategory: input.categories[0] ?? null,
    spotlightProduct: getStrongestProductOpportunity(rankedProducts),
    rankedProducts,
    offerBoardProducts: rankedProducts.slice(0, offerBoardCount),
    campaignCategories: input.categories.slice(0, categoryCampaignCount),
    campaignProducts: rankedProducts.slice(0, productCampaignCount),
  };
}
