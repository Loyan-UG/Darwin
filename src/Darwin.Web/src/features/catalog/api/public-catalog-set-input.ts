export type PublicProductSetInput = {
  culture?: string;
  categorySlug?: string;
  search?: string;
};

export function normalizePublicProductSetInput(input?: PublicProductSetInput) {
  const culture = input?.culture?.trim();
  const categorySlug = input?.categorySlug?.trim();
  const search = input?.search?.trim();

  return {
    culture: culture || undefined,
    categorySlug: categorySlug || undefined,
    search: search || undefined,
  };
}
