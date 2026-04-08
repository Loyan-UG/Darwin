export type ApiDiagnostics = {
  area: string;
  path: string;
  statusCode?: number;
  statusFamily?: ApiStatusFamily;
  requestId?: string;
  traceparent?: string;
  failureKind?: ApiFailureKind;
  retryable?: boolean;
  apiKind?: ApiKind;
  surfaceFamily?: ApiSurfaceFamily;
  surfaceArea?: string;
  attentionLevel?: "medium" | "high";
  suggestedAction?: string;
};

export type ApiKind = "public" | "member" | "auth" | "other";

export type ApiSurfaceFamily =
  | "shell"
  | "public-discovery"
  | "commerce"
  | "member"
  | "auth"
  | "other";

export type ApiStatusFamily =
  | "success"
  | "redirect"
  | "client-error"
  | "server-error"
  | "network-error"
  | "unknown";

export type ApiFailureKind =
  | "network-error"
  | "unauthorized"
  | "not-found"
  | "http-error"
  | "invalid-payload";

const loggedDiagnosticFailures = new Set<string>();

function readHeader(
  response: Pick<Response, "headers">,
  names: string[],
) {
  for (const name of names) {
    const value = response.headers.get(name)?.trim();
    if (value) {
      return value;
    }
  }

  return undefined;
}

function getStatusFamily(statusCode?: number): ApiStatusFamily | undefined {
  if (statusCode === undefined) {
    return undefined;
  }

  if (statusCode >= 200 && statusCode < 300) {
    return "success";
  }

  if (statusCode >= 300 && statusCode < 400) {
    return "redirect";
  }

  if (statusCode >= 400 && statusCode < 500) {
    return "client-error";
  }

  if (statusCode >= 500) {
    return "server-error";
  }

  return "unknown";
}

function isRetryableFailure(
  failureKind: ApiFailureKind,
  statusCode?: number,
) {
  if (failureKind === "network-error" || failureKind === "invalid-payload") {
    return true;
  }

  if (failureKind === "http-error") {
    return statusCode === undefined ? true : statusCode >= 500 || statusCode === 429;
  }

  return false;
}

function getApiKind(path: string): ApiKind {
  if (path.startsWith("/api/v1/public/")) {
    return "public";
  }

  if (path.startsWith("/api/v1/member/") || path.startsWith("/api/v1/customer/")) {
    return "member";
  }

  if (path.startsWith("/api/v1/auth/")) {
    return "auth";
  }

  return "other";
}

function getSurfaceFamily(path: string, area: string): ApiSurfaceFamily {
  if (area === "cms-menu" || path.includes("/cms/menus/")) {
    return "shell";
  }

  if (
    path.includes("/public/cart") ||
    path.includes("/public/checkout") ||
    path.includes("/public/orders/")
  ) {
    return "commerce";
  }

  if (path.includes("/member/") || path.includes("/customer/")) {
    return "member";
  }

  if (path.includes("/auth/")) {
    return "auth";
  }

  if (path.includes("/public/cms") || path.includes("/public/catalog")) {
    return "public-discovery";
  }

  return "other";
}

function getSurfaceArea(path: string, area: string) {
  if (area === "cms-menu" || path.includes("/cms/menus/")) {
    return "menu";
  }

  if (path.includes("/public/cms/pages")) {
    return "cms-pages";
  }

  if (path.includes("/public/catalog/categories")) {
    return "catalog-categories";
  }

  if (path.includes("/public/catalog/products")) {
    return "catalog-products";
  }

  if (path.includes("/public/cart")) {
    return "cart";
  }

  if (path.includes("/public/checkout")) {
    return "checkout";
  }

  if (path.includes("/member/orders")) {
    return "member-orders";
  }

  if (path.includes("/member/invoices")) {
    return "member-invoices";
  }

  if (path.includes("/auth/")) {
    return "auth";
  }

  return area;
}

function getFailureAttentionLevel(
  failureKind: ApiFailureKind,
  statusCode?: number,
): "medium" | "high" {
  if (
    failureKind === "network-error" ||
    failureKind === "invalid-payload" ||
    statusCode === undefined ||
    statusCode >= 500
  ) {
    return "high";
  }

  return "medium";
}

function getFailureSuggestedAction(input: {
  failureKind: ApiFailureKind;
  retryable: boolean;
  surfaceFamily: ApiSurfaceFamily;
}) {
  if (input.failureKind === "network-error") {
    return `inspect-${input.surfaceFamily}-connectivity`;
  }

  if (input.failureKind === "invalid-payload") {
    return `inspect-${input.surfaceFamily}-contract`;
  }

  if (input.failureKind === "unauthorized") {
    return `inspect-${input.surfaceFamily}-access`;
  }

  if (input.failureKind === "not-found") {
    return `inspect-${input.surfaceFamily}-availability`;
  }

  if (input.retryable) {
    return `retry-${input.surfaceFamily}-request`;
  }

  return `inspect-${input.surfaceFamily}-failure`;
}

export function getResponseDiagnostics(
  area: string,
  path: string,
  response: Pick<Response, "status" | "headers">,
): ApiDiagnostics {
  return {
    area,
    path,
    apiKind: getApiKind(path),
    surfaceFamily: getSurfaceFamily(path, area),
    surfaceArea: getSurfaceArea(path, area),
    statusCode: response.status,
    statusFamily: getStatusFamily(response.status),
    requestId: readHeader(response, ["x-request-id", "request-id", "x-correlation-id"]),
    traceparent: readHeader(response, ["traceparent"]),
  };
}

export function createDiagnostics(area: string, path: string): ApiDiagnostics {
  return {
    area,
    path,
    apiKind: getApiKind(path),
    surfaceFamily: getSurfaceFamily(path, area),
    surfaceArea: getSurfaceArea(path, area),
    statusFamily: "network-error",
  };
}

export function withFailureDiagnostics(
  diagnostics: ApiDiagnostics,
  failureKind: ApiFailureKind,
): ApiDiagnostics {
  const retryable = isRetryableFailure(failureKind, diagnostics.statusCode);
  const surfaceFamily = diagnostics.surfaceFamily ?? "other";

  return {
    ...diagnostics,
    failureKind,
    retryable,
    attentionLevel: getFailureAttentionLevel(failureKind, diagnostics.statusCode),
    suggestedAction: getFailureSuggestedAction({
      failureKind,
      retryable,
      surfaceFamily,
    }),
  };
}

export function logApiFailure(
  diagnostics: ApiDiagnostics,
  detail: unknown,
) {
  const dedupeKey = [
    diagnostics.area,
    diagnostics.path,
    diagnostics.statusCode ?? "no-status",
    diagnostics.failureKind ?? "no-failure-kind",
    diagnostics.requestId ?? "no-request-id",
    diagnostics.traceparent ?? "no-traceparent",
  ].join("|");

  if (loggedDiagnosticFailures.has(dedupeKey)) {
    return;
  }

  loggedDiagnosticFailures.add(dedupeKey);
  console.error("Darwin.Web API failure", {
    diagnostics,
    detail,
  });
}
