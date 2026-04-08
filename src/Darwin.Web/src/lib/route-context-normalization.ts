export function normalizeCultureArg(culture: string): [string] {
  return [culture.trim()];
}

export function normalizePublicAuthRouteArgs(
  culture: string,
  route: string,
): [string, string] {
  return [culture.trim(), route.trim()];
}

export function normalizePagedRouteArgs(
  culture: string,
  page: number,
  pageSize: number,
): [string, number, number] {
  return [
    culture.trim(),
    Number.isFinite(page) && page > 0 ? Math.floor(page) : 1,
    Number.isFinite(pageSize) && pageSize > 0 ? Math.floor(pageSize) : 1,
  ];
}

export function normalizePagingArgs(
  page: number,
  pageSize: number,
): [number, number] {
  return [
    Number.isFinite(page) && page > 0 ? Math.floor(page) : 1,
    Number.isFinite(pageSize) && pageSize > 0 ? Math.floor(pageSize) : 1,
  ];
}

export function normalizeEntityRouteArgs(
  culture: string,
  id: string,
): [string, string] {
  return [culture.trim(), id.trim()];
}

export function normalizeConfirmationResultArgs(
  orderId: string,
  orderNumber?: string,
): [string, string | undefined] {
  return [orderId.trim(), orderNumber?.trim() || undefined];
}

export function normalizeConfirmationRouteArgs(
  culture: string,
  orderId: string,
  orderNumber?: string,
): [string, string, string | undefined] {
  return [culture.trim(), orderId.trim(), orderNumber?.trim() || undefined];
}
