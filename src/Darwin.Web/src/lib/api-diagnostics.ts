export type ApiDiagnostics = {
  area: string;
  path: string;
  statusCode?: number;
  statusFamily?: ApiStatusFamily;
  requestId?: string;
  traceparent?: string;
  failureKind?: ApiFailureKind;
  retryable?: boolean;
};

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

export function getResponseDiagnostics(
  area: string,
  path: string,
  response: Pick<Response, "status" | "headers">,
): ApiDiagnostics {
  return {
    area,
    path,
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
    statusFamily: "network-error",
  };
}

export function withFailureDiagnostics(
  diagnostics: ApiDiagnostics,
  failureKind: ApiFailureKind,
): ApiDiagnostics {
  return {
    ...diagnostics,
    failureKind,
    retryable: isRetryableFailure(failureKind, diagnostics.statusCode),
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
