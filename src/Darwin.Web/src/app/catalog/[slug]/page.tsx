import { redirect } from "next/navigation";
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
import {
  INFERRED_CULTURE_SEARCH_PARAM,
  localizeHref,
} from "@/lib/locale-routing";
import { buildCatalogProductPath } from "@/lib/entity-paths";
import { getRequestCulture, getSupportedCulturesAsync } from "@/lib/request-culture";

type ProductSearchParams = {
  category?: string;
  visibleQuery?: string;
  visibleState?: string;
  visibleSort?: string;
  mediaState?: string;
  savingsBand?: string;
};

type ProductDetailRouteProps = {
  params: Promise<{
    slug: string;
  }>;
  searchParams?: Promise<ProductSearchParams>;
};

function appendSearchParams(
  href: string,
  searchParams: ProductSearchParams | undefined,
  extraParams?: Record<string, string | undefined>,
) {
  if (!searchParams) {
    const params = new URLSearchParams();
    for (const [key, value] of Object.entries(extraParams ?? {})) {
      if (value) {
        params.set(key, value);
      }
    }

    const query = params.toString();
    return query ? `${href}?${query}` : href;
  }

  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(searchParams)) {
    if (value) {
      params.set(key, value);
    }
  }
  for (const [key, value] of Object.entries(extraParams ?? {})) {
    if (value) {
      params.set(key, value);
    }
  }

  const query = params.toString();
  return query ? `${href}?${query}` : href;
}

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
  const { detailContext } = await getCatalogDetailPageContext(
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
    activeCategory,
    relatedProducts,
    recommendedProducts,
  } = detailContext;
  const product = productResult.data;

  if (!product && productResult.status === "not-found") {
    for (const alternateCulture of await getSupportedCulturesAsync()) {
      if (alternateCulture === culture) {
        continue;
      }

      const alternateContext = await getCatalogDetailPageContext(
        alternateCulture,
        slug,
        category,
        visibleQuery,
        visibleState,
        visibleSort,
        mediaState,
        savingsBand,
      );

      const alternateProduct = alternateContext.detailContext.productResult.data;
      if (alternateProduct) {
        redirect(
    appendSearchParams(buildCatalogProductPath(slug), resolvedSearchParams, {
            culture: alternateCulture,
            [INFERRED_CULTURE_SEARCH_PARAM]: "1",
          }),
        );
      }
    }
  }

  if (product?.slug && product.slug !== slug) {
    redirect(
      appendSearchParams(
      localizeHref(buildCatalogProductPath(product.slug), culture),
        resolvedSearchParams,
      ),
    );
  }

  return (
    <ProductDetailPage
      culture={culture}
      product={product}
      primaryCategory={activeCategory}
      relatedProducts={relatedProducts.length > 0 ? relatedProducts : recommendedProducts}
      status={productResult.status}
    />
  );
}

