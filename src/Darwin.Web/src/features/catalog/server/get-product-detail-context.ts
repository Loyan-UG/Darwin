import "server-only";
import {
  getPublicCategories,
  getPublicProductBySlug,
  getPublicProducts,
} from "@/features/catalog/api/public-catalog";
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
  getContext: (culture: string, slug: string) =>
    productDetailObservationContext(culture, slug),
  getSuccessContext: (result) => ({
    ...summarizeCatalogDetailCoreHealth(result),
    relatedStatus: result.relatedProductsResult?.status ?? "not-requested",
    relatedCount: result.relatedProducts.length,
  }),
  load: async (culture: string, slug: string) => {
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
    };
  },
});

export async function getProductDetailContext(culture: string, slug: string) {
  return getCachedProductDetailContext(culture, slug);
}
