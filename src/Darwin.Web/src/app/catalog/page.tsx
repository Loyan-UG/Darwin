import { CatalogPage } from "@/components/catalog/catalog-page";
import {
  getPublicCategories,
  getPublicProducts,
} from "@/features/catalog/api/public-catalog";
import { getRequestCulture } from "@/lib/request-culture";

export const metadata = {
  title: "Catalog",
};

type CatalogRouteProps = {
  searchParams?: Promise<{
    category?: string;
    page?: string;
  }>;
};

export default async function CatalogRoute({ searchParams }: CatalogRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const culture = await getRequestCulture();
  const page = Number(resolvedSearchParams?.page ?? "1");
  const safePage = Number.isFinite(page) && page > 0 ? page : 1;
  const activeCategorySlug = resolvedSearchParams?.category?.trim() || undefined;

  const [categoriesResult, productsResult] = await Promise.all([
    getPublicCategories(culture),
    getPublicProducts({
      page: safePage,
      pageSize: 12,
      culture,
      categorySlug: activeCategorySlug,
    }),
  ]);

  return (
    <CatalogPage
      culture={culture}
      categories={categoriesResult.data?.items ?? []}
      products={productsResult.data?.items ?? []}
      activeCategorySlug={activeCategorySlug}
      totalProducts={productsResult.data?.total ?? 0}
      currentPage={safePage}
      pageSize={productsResult.data?.request.pageSize ?? 12}
      dataStatus={{
        categories: categoriesResult.status,
        products: productsResult.status,
      }}
    />
  );
}
