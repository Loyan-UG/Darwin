import "server-only";
import {
  getPublicCategories,
  getPublicProducts,
} from "@/features/catalog/api/public-catalog";
import { getPublicCart } from "@/features/cart/api/public-cart";
import { getAnonymousCartId } from "@/features/cart/cookies";
import { getPublishedPages } from "@/features/cms/api/public-cms";

export async function getPublicAuthStorefrontContext(culture: string) {
  const anonymousCartId = await getAnonymousCartId();
  const [cmsPagesResult, categoriesResult, productsResult, storefrontCartResult] = await Promise.all([
    getPublishedPages({ page: 1, pageSize: 2, culture }),
    getPublicCategories(culture),
    getPublicProducts({ page: 1, pageSize: 3, culture }),
    anonymousCartId
      ? getPublicCart(anonymousCartId)
      : Promise.resolve({ data: null, status: "not-found" as const }),
  ]);

  return {
    cmsPages: cmsPagesResult.data?.items ?? [],
    cmsPagesStatus: cmsPagesResult.status,
    categories: categoriesResult.data?.items.slice(0, 3) ?? [],
    categoriesStatus: categoriesResult.status,
    products: productsResult.data?.items ?? [],
    productsStatus: productsResult.status,
    storefrontCart: storefrontCartResult.data,
    storefrontCartStatus: storefrontCartResult.status,
  };
}
