import { ProductDetailPage } from "@/components/catalog/product-detail-page";
import {
  getPublicCategories,
  getPublicProductBySlug,
  getPublicProducts,
} from "@/features/catalog/api/public-catalog";
import { getPublishedPages } from "@/features/cms/api/public-cms";
import { getCatalogResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { buildSeoMetadata, deriveSeoDescription } from "@/lib/seo";

type ProductDetailRouteProps = {
  params: Promise<{
    slug: string;
  }>;
};

export async function generateMetadata({ params }: ProductDetailRouteProps) {
  const culture = await getRequestCulture();
  const copy = getCatalogResource(culture);
  const { slug } = await params;
  const productResult = await getPublicProductBySlug(slug, culture);
  const product = productResult.data;
  const path = `/catalog/${encodeURIComponent(slug)}`;

  if (!product) {
    return buildSeoMetadata({
      culture,
      title: copy.productUnavailableMetaTitle,
      description: copy.productFallbackMetaDescription,
      path,
      noIndex: true,
    });
  }

  return buildSeoMetadata({
    culture,
    title: product.metaTitle ?? product.name,
    description:
      deriveSeoDescription(
        product.metaDescription,
        product.shortDescription,
        product.fullDescriptionHtml,
      ) ?? copy.productFallbackMetaDescription,
    path,
    imageUrl: product.media[0]?.url ?? product.primaryImageUrl,
    noIndex: productResult.status !== "ok",
  });
}

export default async function ProductDetailRoute({
  params,
}: ProductDetailRouteProps) {
  const culture = await getRequestCulture();
  const { slug } = await params;
  const [productResult, categoriesResult, cmsPagesResult] = await Promise.all([
    getPublicProductBySlug(slug, culture),
    getPublicCategories(culture),
    getPublishedPages({
      page: 1,
      pageSize: 3,
      culture,
    }),
  ]);
  const activeCategory =
    categoriesResult.data?.items.find(
      (category) => category.id === productResult.data?.primaryCategoryId,
    ) ?? null;
  const relatedProductsResult =
    activeCategory && productResult.data
      ? await getPublicProducts({
          page: 1,
          pageSize: 5,
          culture,
          categorySlug: activeCategory.slug,
        })
      : null;
  const relatedProducts =
    relatedProductsResult?.data?.items.filter(
      (product) => product.slug !== productResult.data?.slug,
    ) ?? [];

  return (
    <ProductDetailPage
      culture={culture}
      product={productResult.data}
      primaryCategory={activeCategory}
      relatedProducts={relatedProducts}
      cmsPages={cmsPagesResult.data?.items ?? []}
      status={productResult.status}
      relatedProductsStatus={relatedProductsResult?.status}
      cmsPagesStatus={cmsPagesResult.status}
    />
  );
}
