import { availableThemes, type ThemeId } from "@/themes/registry";

type SiteRuntimeConfig = {
  webApiBaseUrl: string;
  siteUrl: string;
  mainMenuName: string;
  footerMenuName: string;
  theme: ThemeId;
  defaultCulture: string;
  supportedCultures: string[];
  cultureCookieName: string;
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

export function getSiteRuntimeConfig(): SiteRuntimeConfig {
  const defaultCulture = process.env.DARWIN_WEB_CULTURE ?? "de-DE";
  const supportedCultures = parseSupportedCultures(
    process.env.DARWIN_WEB_SUPPORTED_CULTURES,
  );

  return {
    webApiBaseUrl: trimTrailingSlash(
      process.env.DARWIN_WEBAPI_BASE_URL ?? "http://localhost:5134",
    ),
    siteUrl: trimTrailingSlash(
      process.env.DARWIN_WEB_SITE_URL ?? "http://localhost:3000",
    ),
    mainMenuName: process.env.DARWIN_WEB_MAIN_MENU_NAME ?? "main-navigation",
    footerMenuName: process.env.DARWIN_WEB_FOOTER_MENU_NAME ?? "Footer",
    theme: parseTheme(process.env.DARWIN_WEB_THEME),
    defaultCulture: supportedCultures.includes(defaultCulture)
      ? defaultCulture
      : supportedCultures[0],
    supportedCultures,
    cultureCookieName:
      process.env.DARWIN_WEB_CULTURE_COOKIE_NAME ?? "darwin-web-culture",
  };
}
