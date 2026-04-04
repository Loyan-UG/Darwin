import { ProductDetailPage } from "@/components/catalog/product-detail-page";
import {
  readCatalogMediaState,
  readCatalogSavingsBand,
  readCatalogVisibleSort,
  readCatalogVisibleState,
} from "@/features/catalog/discovery";
import { getCatalogDetailPageContext } from "@/features/catalog/server/get-catalog-page-context";
import { getProductSeoMetadata } from "@/features/catalog/server/get-product-seo-metadata";
import {
  readSearchTextParam,
} from "@/features/checkout/helpers";
import { getRequestCulture } from "@/lib/request-culture";

type ProductDetailRouteProps = {
  params: Promise<{
    slug: string;
  }>;
  searchParams?: Promise<{
    category?: string;
    visibleQuery?: string;
    visibleState?: string;
    visibleSort?: string;
    mediaState?: string;
    savingsBand?: string;
  }>;
};

export async function generateMetadata({ params }: ProductDetailRouteProps) {
  const culture = await getRequestCulture();
  const { slug } = await params;
  const { metadata } = await getProductSeoMetadata(culture, slug);
  return metadata;
}

export default async function ProductDetailRoute({
  params,
  searchParams,
}: ProductDetailRouteProps) {
  const culture = await getRequestCulture();
  const { slug } = await params;
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const category = readSearchTextParam(resolvedSearchParams?.category, 80);
  const visibleQuery = readSearchTextParam(
    resolvedSearchParams?.visibleQuery,
    80,
  );
  const visibleState = readCatalogVisibleState(
    resolvedSearchParams?.visibleState,
  );
  const visibleSort = readCatalogVisibleSort(resolvedSearchParams?.visibleSort);
  const mediaState = readCatalogMediaState(resolvedSearchParams?.mediaState);
  const savingsBand = readCatalogSavingsBand(resolvedSearchParams?.savingsBand);
  const { detailContext, continuationSlice } = await getCatalogDetailPageContext(
    culture,
    slug,
    category,
    visibleQuery,
    visibleState,
    visibleSort,
    mediaState,
    savingsBand,
  );
  const {
    productResult,
    categoriesResult,
    activeCategory,
    relatedProductsResult,
    relatedProducts,
    reviewProductsResult,
    reviewProducts,
  } = detailContext;

  return (
    <ProductDetailPage
      culture={culture}
      product={productResult.data}
      categories={categoriesResult.data?.items ?? []}
      primaryCategory={activeCategory}
      reviewWindow={{
        category,
        visibleQuery,
        visibleState,
        visibleSort,
        mediaState,
        savingsBand,
      }}
      relatedProducts={relatedProducts}
      reviewProducts={reviewProducts}
      cmsPages={continuationSlice.cmsPages}
      cartSummary={continuationSlice.cartSummary}
      status={productResult.status}
      relatedProductsStatus={relatedProductsResult?.status}
      reviewProductsStatus={reviewProductsResult?.status}
      cmsPagesStatus={continuationSlice.cmsPagesStatus}
    />
  );
}

