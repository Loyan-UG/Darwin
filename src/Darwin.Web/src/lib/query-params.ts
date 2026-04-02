export function serializeQueryParams(
  params: Record<string, string | number | boolean | undefined | null>,
) {
  const searchParams = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === "") {
      continue;
    }

    searchParams.set(key, String(value));
  }

  return searchParams.toString();
}

export function cloneSearchParams(
  value: URLSearchParams | { toString(): string } | string,
) {
  return new URLSearchParams(
    typeof value === "string" ? value : value.toString(),
  );
}

export function buildQuerySuffix(
  params: Record<string, string | number | boolean | undefined | null>,
) {
  const serialized = serializeQueryParams(params);
  return serialized ? `?${serialized}` : "";
}
