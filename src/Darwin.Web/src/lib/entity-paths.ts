export function buildOrderPath(id: string) {
  return `/orders/${encodeURIComponent(id)}`;
}

export function buildInvoicePath(id: string) {
  return `/invoices/${encodeURIComponent(id)}`;
}

export function buildLoyaltyBusinessPath(businessId: string) {
  return `/loyalty/${encodeURIComponent(businessId)}`;
}

export function buildCatalogProductPath(slug: string) {
  return `/catalog/${encodeURIComponent(slug)}`;
}

export function buildCmsPagePath(slug: string) {
  return `/page/${encodeURIComponent(slug)}`;
}
