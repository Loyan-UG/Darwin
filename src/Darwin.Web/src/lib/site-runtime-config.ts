import "server-only";

type SiteRuntimeConfig = {
  webApiBaseUrl: string;
  mainMenuName: string;
  culture: string;
};

function trimTrailingSlash(value: string) {
  return value.endsWith("/") ? value.slice(0, -1) : value;
}

export function getSiteRuntimeConfig(): SiteRuntimeConfig {
  return {
    webApiBaseUrl: trimTrailingSlash(
      process.env.DARWIN_WEBAPI_BASE_URL ?? "http://localhost:5134",
    ),
    mainMenuName: process.env.DARWIN_WEB_MAIN_MENU_NAME ?? "main-navigation",
    culture: process.env.DARWIN_WEB_CULTURE ?? "de-DE",
  };
}
