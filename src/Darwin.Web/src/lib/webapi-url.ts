import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

export function toWebApiUrl(path: string) {
  if (!path) {
    return "";
  }

  if (/^https?:\/\//i.test(path)) {
    return path;
  }

  const { webApiBaseUrl } = getSiteRuntimeConfig();
  return `${webApiBaseUrl}${path.startsWith("/") ? path : `/${path}`}`;
}
