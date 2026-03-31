import { ProductDetailPage } from "@/components/catalog/product-detail-page";
import { getPublicProductBySlug } from "@/features/catalog/api/public-catalog";

type ProductDetailRouteProps = {
  params: Promise<{
    slug: string;
  }>;
};

export async function generateMetadata({ params }: ProductDetailRouteProps) {
  const { slug } = await params;
  const productResult = await getPublicProductBySlug(slug);
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
  const { slug } = await params;
  const productResult = await getPublicProductBySlug(slug);

  return (
    <ProductDetailPage
      product={productResult.data}
      status={productResult.status}
    />
  );
}
