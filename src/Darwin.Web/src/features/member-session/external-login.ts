import "server-only";
import { fetchPublicJson } from "@/lib/api/fetch-public-json";

type AppBootstrapResponse = {
  googleExternalLoginEnabled?: boolean;
  googleExternalLoginWebClientId?: string | null;
};

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

  const bootstrap = await fetchPublicJson<AppBootstrapResponse>(
    "/api/v1/meta/bootstrap",
    "app-bootstrap",
  );

  const bootstrapGoogleClientId = normalizeClientId(
    bootstrap.status === "ok" ? bootstrap.data?.googleExternalLoginWebClientId : null,
  );
  const googleWebClientId = bootstrapGoogleClientId ?? envGoogleClientId;

  return {
    googleEnabled:
      Boolean(googleWebClientId) &&
      (bootstrap.status !== "ok" ||
        bootstrap.data?.googleExternalLoginEnabled === true ||
        Boolean(envGoogleClientId)),
    googleWebClientId,
    microsoftEnabled: false,
  };
}
