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

type ProductDetailReviewWindow = {
  category?: string;
  visibleQuery?: string;
  visibleState?: string;
  visibleSort?: string;
  mediaState?: string;
  savingsBand?: string;
};

type ProductDetailSupportWorkflowSource = {
  relatedProductsResult?: { status: string } | null;
  relatedProducts: Array<unknown>;
  reviewProductsResult?: { status: string } | null;
  reviewProducts: Array<unknown>;
};

function normalizeReviewWindow(reviewWindow?: ProductDetailReviewWindow) {
  return {
    category: reviewWindow?.category?.trim() || undefined,
    visibleQuery: reviewWindow?.visibleQuery?.trim() || undefined,
    visibleState: readCatalogVisibleState(reviewWindow?.visibleState),
    visibleSort: readCatalogVisibleSort(reviewWindow?.visibleSort),
    mediaState: readCatalogMediaState(reviewWindow?.mediaState),
    savingsBand: readCatalogSavingsBand(reviewWindow?.savingsBand),
  };
}

export function summarizeProductDetailSupportWorkflow(
  result: ProductDetailSupportWorkflowSource,
) {
  return `related:${result.relatedProductsResult?.status ?? "not-requested"}:${result.relatedProducts.length}|review:${result.reviewProductsResult?.status ?? "not-requested"}:${result.reviewProducts.length}`;
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
    reviewWindow?: ProductDetailReviewWindow,
  ): [string, string, ProductDetailReviewWindow] => [
    culture,
    slug,
    normalizeReviewWindow(reviewWindow),
  ],
  getContext: (
    culture: string,
    slug: string,
    reviewWindow?: ProductDetailReviewWindow,
  ) => {
    const normalizedReviewWindow = normalizeReviewWindow(reviewWindow);

    return {
      ...productDetailObservationContext(culture, slug),
      categorySlug: normalizedReviewWindow.category ?? null,
      visibleQuery: normalizedReviewWindow.visibleQuery ?? null,
      visibleState:
        normalizedReviewWindow.visibleState !== "all"
          ? normalizedReviewWindow.visibleState
          : null,
      visibleSort:
        normalizedReviewWindow.visibleSort !== "featured"
          ? normalizedReviewWindow.visibleSort
          : null,
      mediaState:
        normalizedReviewWindow.mediaState !== "all"
          ? normalizedReviewWindow.mediaState
          : null,
      savingsBand:
        normalizedReviewWindow.savingsBand !== "all"
          ? normalizedReviewWindow.savingsBand
          : null,
    };
  },
  getSuccessContext: (result) => ({
    ...summarizeCatalogDetailCoreHealth(result),
    productDetailSupportWorkflowFootprint:
      summarizeProductDetailSupportWorkflow(result),
    relatedStatus: result.relatedProductsResult?.status ?? "not-requested",
    relatedCount: result.relatedProducts.length,
    reviewStatus: result.reviewProductsResult?.status ?? "not-requested",
    reviewCount: result.reviewProducts.length,
  }),
  load: async (
    culture: string,
    slug: string,
    reviewWindow?: ProductDetailReviewWindow,
  ) => {
    const normalizedReviewWindow = normalizeReviewWindow(reviewWindow);
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
    const reviewCategorySlug =
      normalizedReviewWindow.category ?? activeCategory?.slug;
    const reviewProductsResult =
      productResult.data && reviewCategorySlug
        ? await getCatalogBrowseSet(
            culture,
            reviewCategorySlug,
            normalizedReviewWindow.visibleQuery,
          )
        : null;
    const reviewProducts =
      reviewProductsResult?.status === "ok" && reviewProductsResult.data
        ? sortCatalogVisibleProducts(
            filterCatalogVisibleProducts(
              reviewProductsResult.data.items,
              normalizedReviewWindow.visibleState,
              undefined,
              normalizedReviewWindow.mediaState,
              normalizedReviewWindow.savingsBand,
            ),
            normalizedReviewWindow.visibleSort,
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
      reviewProductsResult,
      reviewProducts,
    };
  },
});

export async function getProductDetailContext(
  culture: string,
  slug: string,
  reviewWindow?: ProductDetailReviewWindow,
) {
  return getCachedProductDetailContext(
    culture,
    slug,
    normalizeReviewWindow(reviewWindow),
  );
}

