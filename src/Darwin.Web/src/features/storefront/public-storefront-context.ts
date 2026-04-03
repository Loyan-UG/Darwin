import type { PublicApiFetchResult, PublicApiFetchStatus } from "@/lib/api/fetch-public-json";
import type { PublicCartSummary, CartDisplaySnapshot } from "@/features/cart/types";
import type { PublicCategorySummary, PagedResponse, PublicProductSummary } from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";

export type StorefrontContinuationContextShape = {
  cmsPagesResult: PublicApiFetchResult<PagedResponse<PublicPageSummary>>;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: PublicApiFetchStatus;
  categoriesResult: PublicApiFetchResult<PagedResponse<PublicCategorySummary>>;
  categories: PublicCategorySummary[];
  categoriesStatus: PublicApiFetchStatus;
  productsResult: PublicApiFetchResult<PagedResponse<PublicProductSummary>>;
  products: PublicProductSummary[];
  productsStatus: PublicApiFetchStatus;
};

export type StorefrontShoppingContextShape = {
  cartResult: {
    data: PublicCartSummary | null;
    status: PublicApiFetchStatus | "not-found";
  };
  cartSnapshots: CartDisplaySnapshot[];
  cartLinkedProductSlugs: string[];
};

export function mergePublicStorefrontContext(
  continuationContext: StorefrontContinuationContextShape,
  shoppingContext: StorefrontShoppingContextShape,
) {
  return {
    ...continuationContext,
    storefrontCart: shoppingContext.cartResult.data,
    storefrontCartStatus: shoppingContext.cartResult.status,
    cartSnapshots: shoppingContext.cartSnapshots,
    cartLinkedProductSlugs: shoppingContext.cartLinkedProductSlugs,
  };
}

export type PublicStorefrontContext = ReturnType<typeof mergePublicStorefrontContext>;
