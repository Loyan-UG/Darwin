import "server-only";
import { getPublicSiteRuntimeConfig } from "@/lib/public-site-runtime-config";

function normalizeClientId(value: string | null | undefined) {
  const trimmed = value?.trim();
  return trimmed && trimmed.endsWith(".apps.googleusercontent.com")
    ? trimmed
    : null;
}

export type ExternalLoginConfig = {
  googleEnabled: boolean;
  googleWebClientId: string | null;
  microsoftEnabled: false;
};

export async function getExternalLoginConfig(): Promise<ExternalLoginConfig> {
  const envGoogleClientId = normalizeClientId(
    process.env.DARWIN_WEB_GOOGLE_EXTERNAL_LOGIN_CLIENT_ID,
  );

  const runtimeConfig = await getPublicSiteRuntimeConfig();

  const runtimeGoogleClientId = normalizeClientId(
    runtimeConfig.googleExternalLoginWebClientId,
  );
  const googleWebClientId = runtimeGoogleClientId ?? envGoogleClientId;

  return {
    googleEnabled:
      Boolean(googleWebClientId) &&
      (runtimeConfig.googleExternalLoginEnabled === true || Boolean(envGoogleClientId)),
    googleWebClientId,
    microsoftEnabled: false,
  };
}
