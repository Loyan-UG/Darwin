import { ProductDetailPage } from "@/components/catalog/product-detail-page";
import {
  getPublicCategories,
  getPublicProductBySlug,
} from "@/features/catalog/api/public-catalog";
import { getRequestCulture } from "@/lib/request-culture";

type ProductDetailRouteProps = {
  params: Promise<{
    slug: string;
  }>;
};

export async function generateMetadata({ params }: ProductDetailRouteProps) {
  const culture = await getRequestCulture();
  const { slug } = await params;
  const productResult = await getPublicProductBySlug(slug, culture);
  const product = productResult.data;

  if (!product) {
    return {
      title: "Product unavailable",
    };
  }

  return {
    title: product.metaTitle ?? product.name,
    description:
      product.metaDescription ??
      product.shortDescription ??
      "Storefront product detail delivered from Darwin.WebApi.",
  };
}

export default async function ProductDetailRoute({
  params,
}: ProductDetailRouteProps) {
  const culture = await getRequestCulture();
  const { slug } = await params;
  const [productResult, categoriesResult] = await Promise.all([
    getPublicProductBySlug(slug, culture),
    getPublicCategories(culture),
  ]);
  const activeCategory =
    categoriesResult.data?.items.find(
      (category) => category.id === productResult.data?.primaryCategoryId,
    ) ?? null;

  return (
    <ProductDetailPage
      culture={culture}
      product={productResult.data}
      primaryCategory={activeCategory}
      status={productResult.status}
    />
  );
}
