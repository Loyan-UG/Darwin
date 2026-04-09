export function canonicalizeLanguageAlternates(
  alternates?: Record<string, string>,
) {
  if (!alternates) {
    return undefined;
  }

  const entries = Object.entries(alternates)
    .filter(([, path]) => typeof path === "string" && path.length > 0)
    .sort(([leftCulture], [rightCulture]) => {
      if (leftCulture === rightCulture) {
        return 0;
      }

      if (leftCulture === "x-default") {
        return -1;
      }

      if (rightCulture === "x-default") {
        return 1;
      }

      return leftCulture.localeCompare(rightCulture);
    });

  return entries.length > 0 ? Object.fromEntries(entries) : undefined;
}
