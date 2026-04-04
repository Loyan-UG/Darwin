import "server-only";
import { Agent } from "undici";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

type WebApiRequestInit = RequestInit & {
  dispatcher?: Agent;
};

const insecureLocalTlsAgent = new Agent({
  connect: {
    rejectUnauthorized: false,
  },
});

function canUseInsecureTls(url: string) {
  const { allowInsecureWebApiTls } = getSiteRuntimeConfig();
  if (!allowInsecureWebApiTls) {
    return false;
  }

  try {
    return new URL(url).protocol === "https:";
  } catch {
    return false;
  }
}

export function buildWebApiFetchInit(
  url: string,
  init?: RequestInit,
): WebApiRequestInit {
  if (!canUseInsecureTls(url)) {
    return { ...(init ?? {}) };
  }

  return {
    ...(init ?? {}),
    dispatcher: insecureLocalTlsAgent,
  };
}
