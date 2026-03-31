import { CatalogPage } from "@/components/catalog/catalog-page";
import {
  getPublicCategories,
  getPublicProducts,
} from "@/features/catalog/api/public-catalog";

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
  const page = Number(resolvedSearchParams?.page ?? "1");
  const safePage = Number.isFinite(page) && page > 0 ? page : 1;
  const activeCategorySlug = resolvedSearchParams?.category?.trim() || undefined;

  const [categoriesResult, productsResult] = await Promise.all([
    getPublicCategories(),
    getPublicProducts({
      page: safePage,
      pageSize: 12,
      categorySlug: activeCategorySlug,
    }),
  ]);

  return (
    <CatalogPage
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
