import type { PublicStorefrontContext } from "@/features/storefront/public-storefront-context";

type SliceOptions = {
  cmsCount?: number;
  categoryCount?: number;
  productCount?: number;
};

export function createStorefrontCartSummary(
  storefrontContext: PublicStorefrontContext,
) {
  if (!storefrontContext.storefrontCart) {
    return null;
  }

  return {
    status: storefrontContext.storefrontCartStatus,
    itemCount: storefrontContext.storefrontCart.items.length,
    currency: storefrontContext.storefrontCart.currency,
    grandTotalGrossMinor: storefrontContext.storefrontCart.grandTotalGrossMinor,
  };
}

export function createStorefrontContinuationSlice(
  storefrontContext: PublicStorefrontContext,
  options?: SliceOptions,
) {
  return {
    cmsPages:
      typeof options?.cmsCount === "number"
        ? storefrontContext.cmsPages.slice(0, options.cmsCount)
        : storefrontContext.cmsPages,
    cmsPagesStatus: storefrontContext.cmsPagesStatus,
    categories:
      typeof options?.categoryCount === "number"
        ? storefrontContext.categories.slice(0, options.categoryCount)
        : storefrontContext.categories,
    categoriesStatus: storefrontContext.categoriesStatus,
    products:
      typeof options?.productCount === "number"
        ? storefrontContext.products.slice(0, options.productCount)
        : storefrontContext.products,
    productsStatus: storefrontContext.productsStatus,
    cartSummary: createStorefrontCartSummary(storefrontContext),
  };
}

export function createStorefrontContinuationProps(
  storefrontContext: PublicStorefrontContext,
  options?: SliceOptions,
) {
  const slice = createStorefrontContinuationSlice(storefrontContext, options);

  return {
    cmsPages: slice.cmsPages,
    cmsPagesStatus: slice.cmsPagesStatus,
    categories: slice.categories,
    categoriesStatus: slice.categoriesStatus,
    products: slice.products,
    productsStatus: slice.productsStatus,
  };
}

export function createStorefrontContinuationWithCartProps(
  storefrontContext: PublicStorefrontContext,
  options?: SliceOptions,
) {
  return {
    ...createStorefrontContinuationProps(storefrontContext, options),
    storefrontCart: storefrontContext.storefrontCart,
    storefrontCartStatus: storefrontContext.storefrontCartStatus,
  };
}

export function createStorefrontCartProps(
  storefrontContext: PublicStorefrontContext,
) {
  return {
    storefrontCart: storefrontContext.storefrontCart,
  };
}

export function createStorefrontContinuationWithCartAndLinkedProps(
  storefrontContext: PublicStorefrontContext,
  options?: SliceOptions,
) {
  return {
    ...createStorefrontContinuationWithCartProps(storefrontContext, options),
    cartLinkedProductSlugs: storefrontContext.cartLinkedProductSlugs,
  };
}
