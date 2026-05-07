import { availableThemes, type ThemeId } from "@/themes/registry";

type SiteRuntimeConfig = {
  webApiBaseUrl: string;
  siteUrl: string;
  mainMenuName: string;
  footerMenuName: string;
  allowInsecureWebApiTls: boolean;
  theme: ThemeId;
  defaultCulture: string;
  supportedCultures: string[];
  cultureCookieName: string;
  footerColumnCount: number;
};

function trimTrailingSlash(value: string) {
  return value.endsWith("/") ? value.slice(0, -1) : value;
}

function parseSupportedCultures(value?: string) {
  const items = (value ?? "de-DE,en-US")
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);

  return items.length > 0 ? Array.from(new Set(items)) : ["de-DE", "en-US"];
}

const availableThemeIds = new Set<string>(availableThemes.map((theme) => theme.id));

function parseTheme(value?: string): ThemeId {
  if (value && availableThemeIds.has(value)) {
    return value as ThemeId;
  }

  return availableThemes[0].id;
}

function parseFooterColumnCount(value?: string) {
  const parsed = Number.parseInt(value ?? "", 10);
  if (!Number.isFinite(parsed)) {
    return 2;
  }

  return Math.min(Math.max(parsed, 2), 6);
}

function shouldAllowInsecureWebApiTls(webApiBaseUrl: string) {
  if (process.env.DARWIN_WEB_ALLOW_INSECURE_WEBAPI_TLS === "true") {
    return true;
  }

  if (process.env.DARWIN_WEB_ALLOW_INSECURE_WEBAPI_TLS === "false") {
    return false;
  }

  try {
    const url = new URL(webApiBaseUrl);
    const isLocalHost =
      url.hostname === "localhost" ||
      url.hostname === "127.0.0.1" ||
      url.hostname === "::1";

    return process.env.NODE_ENV !== "production" && isLocalHost;
  } catch {
    return false;
  }
}

export function getSiteRuntimeConfig(): SiteRuntimeConfig {
  const defaultCulture = process.env.DARWIN_WEB_CULTURE ?? "de-DE";
  const supportedCultures = parseSupportedCultures(
    process.env.DARWIN_WEB_SUPPORTED_CULTURES,
  );
  const webApiBaseUrl = trimTrailingSlash(
    process.env.DARWIN_WEBAPI_BASE_URL ?? "http://localhost:5134",
  );

  return {
    webApiBaseUrl,
    siteUrl: trimTrailingSlash(
      process.env.DARWIN_WEB_SITE_URL ?? "http://localhost:3000",
    ),
    mainMenuName: process.env.DARWIN_WEB_MAIN_MENU_NAME ?? "main-navigation",
    footerMenuName: process.env.DARWIN_WEB_FOOTER_MENU_NAME ?? "Footer",
    allowInsecureWebApiTls: shouldAllowInsecureWebApiTls(webApiBaseUrl),
    theme: parseTheme(process.env.DARWIN_WEB_THEME),
    defaultCulture: supportedCultures.includes(defaultCulture)
      ? defaultCulture
      : supportedCultures[0],
    supportedCultures,
    cultureCookieName:
      process.env.DARWIN_WEB_CULTURE_COOKIE_NAME ?? "darwin-web-culture",
    footerColumnCount: parseFooterColumnCount(
      process.env.DARWIN_WEB_FOOTER_COLUMN_COUNT,
    ),
  };
}
