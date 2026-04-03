export type PublicApiCachePolicy = {
  revalidate: number;
  tags: string[];
};

const PUBLIC_API_REVALIDATE_BY_KEY: Record<string, number> = {
  "cms-menu": 900,
  "cms-page": 300,
  "cms-pages": 180,
  "catalog-categories": 900,
  "catalog-products": 120,
  "catalog-product-detail": 180,
};

export function getPublicApiCachePolicy(
  key: string,
  path: string,
): PublicApiCachePolicy {
  const revalidate = PUBLIC_API_REVALIDATE_BY_KEY[key] ?? 60;

  return {
    revalidate,
    tags: [`public:${key}`, `path:${path}`],
  };
}
