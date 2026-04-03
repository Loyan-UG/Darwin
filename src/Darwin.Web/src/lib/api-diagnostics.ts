export type ApiDiagnostics = {
  area: string;
  path: string;
  statusCode?: number;
  requestId?: string;
  traceparent?: string;
};

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

export function getResponseDiagnostics(
  area: string,
  path: string,
  response: Pick<Response, "status" | "headers">,
): ApiDiagnostics {
  return {
    area,
    path,
    statusCode: response.status,
    requestId: readHeader(response, ["x-request-id", "request-id", "x-correlation-id"]),
    traceparent: readHeader(response, ["traceparent"]),
  };
}

export function createDiagnostics(area: string, path: string): ApiDiagnostics {
  return {
    area,
    path,
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
