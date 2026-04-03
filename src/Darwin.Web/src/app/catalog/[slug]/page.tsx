import { ProductDetailPage } from "@/components/catalog/product-detail-page";
import {
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
  const { detailContext, continuationSlice } = await getCatalogDetailPageContext(
    culture,
    slug,
  );
  const {
    productResult,
    categoriesResult,
    activeCategory,
    relatedProductsResult,
    relatedProducts,
  } = detailContext;

  return (
    <ProductDetailPage
      culture={culture}
      product={productResult.data}
      categories={categoriesResult.data?.items ?? []}
      primaryCategory={activeCategory}
      reviewWindow={{
        category: readSearchTextParam(resolvedSearchParams?.category, 80),
        visibleQuery: readSearchTextParam(resolvedSearchParams?.visibleQuery, 80),
        visibleState: readCatalogVisibleState(
          resolvedSearchParams?.visibleState,
        ),
        visibleSort: readCatalogVisibleSort(resolvedSearchParams?.visibleSort),
      }}
      relatedProducts={relatedProducts}
      cmsPages={continuationSlice.cmsPages}
      cartSummary={continuationSlice.cartSummary}
      status={productResult.status}
      relatedProductsStatus={relatedProductsResult?.status}
      cmsPagesStatus={continuationSlice.cmsPagesStatus}
    />
  );
}

