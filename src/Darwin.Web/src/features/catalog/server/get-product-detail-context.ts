import "server-only";
import {
  getPublicCategories,
  getPublicProductBySlug,
  getPublicProducts,
} from "@/features/catalog/api/public-catalog";
import {
  filterCatalogVisibleProducts,
  readCatalogMediaState,
  readCatalogSavingsBand,
  readCatalogVisibleSort,
  readCatalogVisibleState,
  sortCatalogVisibleProducts,
} from "@/features/catalog/discovery";
import { getCatalogBrowseSet } from "@/features/catalog/server/get-catalog-browse-set";
import {
  createCachedObservedLoader,
  createObservedLoader,
} from "@/lib/observed-loader";
import {
  summarizeCatalogDetailCoreHealth,
  summarizeProductDetailRelatedHealth,
} from "@/lib/route-health";
import {
  productDetailObservationContext,
  productDetailRelatedObservationContext,
} from "@/lib/route-observation-context";

type ProductDetailRecommendationWindow = {
  category?: string;
  visibleQuery?: string;
  visibleState?: string;
  visibleSort?: string;
  mediaState?: string;
  savingsBand?: string;
};

type ProductDetailContextFootprintSource = {
  relatedProductsResult?: { status: string } | null;
  relatedProducts: Array<unknown>;
  recommendedProductsResult?: { status: string } | null;
  recommendedProducts: Array<unknown>;
};

function normalizeRecommendationWindow(
  recommendationWindow?: ProductDetailRecommendationWindow,
) {
  return {
    category: recommendationWindow?.category?.trim() || undefined,
    visibleQuery: recommendationWindow?.visibleQuery?.trim() || undefined,
    visibleState: readCatalogVisibleState(recommendationWindow?.visibleState),
    visibleSort: readCatalogVisibleSort(recommendationWindow?.visibleSort),
    mediaState: readCatalogMediaState(recommendationWindow?.mediaState),
    savingsBand: readCatalogSavingsBand(recommendationWindow?.savingsBand),
  };
}

export function summarizeProductDetailContextFootprint(
  result: ProductDetailContextFootprintSource,
) {
  return `related:${result.relatedProductsResult?.status ?? "not-requested"}:${result.relatedProducts.length}|recommended:${result.recommendedProductsResult?.status ?? "not-requested"}:${result.recommendedProducts.length}`;
}

const loadProductDetailCoreContext = createCachedObservedLoader({
  area: "product-detail",
  operation: "load-core-context",
  thresholdMs: 250,
  getContext: (culture: string, slug: string) =>
    productDetailObservationContext(culture, slug),
  getSuccessContext: summarizeCatalogDetailCoreHealth,
  load: async (culture: string, slug: string) => {
    const [productResult, categoriesResult] = await Promise.all([
      getPublicProductBySlug(slug, culture),
      getPublicCategories(culture),
    ]);

    return {
      productResult,
      categoriesResult,
    };
  },
});

const loadProductDetailRelatedProducts = createObservedLoader({
  area: "product-detail",
  operation: "load-related-products",
  thresholdMs: 250,
  getContext: (culture: string, slug: string, categorySlug: string) =>
    productDetailRelatedObservationContext(culture, slug, categorySlug),
  getSuccessContext: summarizeProductDetailRelatedHealth,
  load: (culture: string, _slug: string, categorySlug: string) =>
    getPublicProducts({
      page: 1,
      pageSize: 5,
      culture,
      categorySlug,
    }),
});

const getCachedProductDetailContext = createCachedObservedLoader({
  area: "product-detail",
  operation: "load-detail-context",
  thresholdMs: 275,
  normalizeArgs: (
    culture: string,
    slug: string,
    recommendationWindow?: ProductDetailRecommendationWindow,
  ): [string, string, ProductDetailRecommendationWindow] => [
    culture,
    slug,
    normalizeRecommendationWindow(recommendationWindow),
  ],
  getContext: (
    culture: string,
    slug: string,
    recommendationWindow?: ProductDetailRecommendationWindow,
  ) => {
    const normalizedRecommendationWindow =
      normalizeRecommendationWindow(recommendationWindow);

    return {
      ...productDetailObservationContext(culture, slug),
      categorySlug: normalizedRecommendationWindow.category ?? null,
      visibleQuery: normalizedRecommendationWindow.visibleQuery ?? null,
      visibleState:
        normalizedRecommendationWindow.visibleState !== "all"
          ? normalizedRecommendationWindow.visibleState
          : null,
      visibleSort:
        normalizedRecommendationWindow.visibleSort !== "featured"
          ? normalizedRecommendationWindow.visibleSort
          : null,
      mediaState:
        normalizedRecommendationWindow.mediaState !== "all"
          ? normalizedRecommendationWindow.mediaState
          : null,
      savingsBand:
        normalizedRecommendationWindow.savingsBand !== "all"
          ? normalizedRecommendationWindow.savingsBand
          : null,
    };
  },
  getSuccessContext: (result) => ({
    ...summarizeCatalogDetailCoreHealth(result),
    productDetailContextFootprint:
      summarizeProductDetailContextFootprint(result),
    relatedStatus: result.relatedProductsResult?.status ?? "not-requested",
    relatedCount: result.relatedProducts.length,
    recommendedStatus:
      result.recommendedProductsResult?.status ?? "not-requested",
    recommendedCount: result.recommendedProducts.length,
  }),
  load: async (
    culture: string,
    slug: string,
    recommendationWindow?: ProductDetailRecommendationWindow,
  ) => {
    const normalizedRecommendationWindow =
      normalizeRecommendationWindow(recommendationWindow);
    const { productResult, categoriesResult } = await loadProductDetailCoreContext(
      culture,
      slug,
    );
    const activeCategory =
      categoriesResult.data?.items.find(
        (category) => category.id === productResult.data?.primaryCategoryId,
      ) ?? null;
    const relatedProductsResult =
      activeCategory && productResult.data
        ? await loadProductDetailRelatedProducts(culture, slug, activeCategory.slug)
        : null;
    const recommendedCategorySlug =
      normalizedRecommendationWindow.category ?? activeCategory?.slug;
    const recommendedProductsResult =
      productResult.data && recommendedCategorySlug
        ? await getCatalogBrowseSet(
            culture,
            recommendedCategorySlug,
            normalizedRecommendationWindow.visibleQuery,
          )
        : null;
    const recommendedProducts =
      recommendedProductsResult?.status === "ok" && recommendedProductsResult.data
        ? sortCatalogVisibleProducts(
            filterCatalogVisibleProducts(
              recommendedProductsResult.data.items,
              normalizedRecommendationWindow.visibleState,
              undefined,
              normalizedRecommendationWindow.mediaState,
              normalizedRecommendationWindow.savingsBand,
            ),
            normalizedRecommendationWindow.visibleSort,
          ).filter((product) => product.slug !== productResult.data?.slug)
        : [];
    const relatedProducts =
      relatedProductsResult?.data?.items.filter(
        (product) => product.slug !== productResult.data?.slug,
      ) ?? [];

    return {
      productResult,
      categoriesResult,
      activeCategory,
      relatedProductsResult,
      relatedProducts,
      recommendedProductsResult,
      recommendedProducts,
    };
  },
});

export async function getProductDetailContext(
  culture: string,
  slug: string,
  recommendationWindow?: ProductDetailRecommendationWindow,
) {
  return getCachedProductDetailContext(
    culture,
    slug,
    normalizeRecommendationWindow(recommendationWindow),
  );
}

