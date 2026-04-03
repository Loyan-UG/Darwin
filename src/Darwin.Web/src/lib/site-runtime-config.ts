type SiteRuntimeConfig = {
  webApiBaseUrl: string;
  siteUrl: string;
  mainMenuName: string;
  theme: "grocer" | "atelier";
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

function parseTheme(value?: string): "grocer" | "atelier" {
  return value === "atelier" ? "atelier" : "grocer";
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
    theme: parseTheme(process.env.DARWIN_WEB_THEME),
    defaultCulture: supportedCultures.includes(defaultCulture)
      ? defaultCulture
      : supportedCultures[0],
    supportedCultures,
    cultureCookieName:
      process.env.DARWIN_WEB_CULTURE_COOKIE_NAME ?? "darwin-web-culture",
  };
}
