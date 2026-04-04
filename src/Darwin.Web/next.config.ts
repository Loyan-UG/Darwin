import type { NextConfig } from "next";

function shouldAllowInsecureLocalTls() {
  const override = process.env.DARWIN_WEB_ALLOW_INSECURE_WEBAPI_TLS;
  if (override === "true") {
    return true;
  }

  if (override === "false") {
    return false;
  }

  const baseUrl = process.env.DARWIN_WEBAPI_BASE_URL ?? "http://localhost:5134";

  try {
    const url = new URL(baseUrl);
    const isLocalHost =
      url.hostname === "localhost" ||
      url.hostname === "127.0.0.1" ||
      url.hostname === "::1";

    return process.env.NODE_ENV !== "production" && isLocalHost;
  } catch {
    return false;
  }
}

if (
  shouldAllowInsecureLocalTls() &&
  process.env.NODE_TLS_REJECT_UNAUTHORIZED !== "0"
) {
  process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
}

const nextConfig: NextConfig = {
  /* config options here */
};

export default nextConfig;
