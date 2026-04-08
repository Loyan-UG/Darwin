export type PublishedPageSetInput = {
  culture?: string;
  search?: string;
};

export function normalizePublishedPageSetInput(input?: PublishedPageSetInput) {
  const culture = input?.culture?.trim();
  const search = input?.search?.trim();

  return {
    culture: culture || undefined,
    search: search || undefined,
  };
}
