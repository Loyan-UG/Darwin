import type { PublicProductSummary } from "@/features/catalog/types";

export function filterProductsByExcludedCatalogPaths(
  products: PublicProductSummary[],
  excludedCatalogPaths: Iterable<string>,
) {
  const excluded = new Set(excludedCatalogPaths);
  return products.filter((product) => !excluded.has(`/catalog/${product.slug}`));
}

export function filterProductsByPurchasedNames(
  products: PublicProductSummary[],
  purchasedNames: Iterable<string>,
) {
  const excluded = new Set(
    Array.from(purchasedNames, (name) => name.trim().toLowerCase()).filter(
      (name) => name.length > 0,
    ),
  );

  return products.filter(
    (product) => !excluded.has(product.name.trim().toLowerCase()),
  );
}
