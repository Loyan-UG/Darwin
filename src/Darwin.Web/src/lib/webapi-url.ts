import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

export function getSafeExternalLinkProps() {
  return {
    target: "_blank" as const,
    rel: "noopener noreferrer",
    referrerPolicy: "no-referrer" as const,
  };
}

export function toSafeHttpUrl(path: string) {
  if (!path || !/^https?:\/\//i.test(path)) {
    return "";
  }

  try {
    const url = new URL(path);
    return /^https?:$/i.test(url.protocol) ? url.toString() : "";
  } catch {
    return "";
  }
}

export function toWebApiUrl(path: string) {
  if (!path) {
    return "";
  }

  if (/^https?:\/\//i.test(path)) {
    return toSafeHttpUrl(path);
  }

  if (!path.startsWith("/") || path.startsWith("//")) {
    return "";
  }

  const { webApiBaseUrl } = getSiteRuntimeConfig();

  try {
    return new URL(path, `${webApiBaseUrl}/`).toString();
  } catch {
    return "";
  }
}
