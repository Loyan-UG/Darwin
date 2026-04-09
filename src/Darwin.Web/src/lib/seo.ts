import type { Metadata } from "next";
import { getSharedResource } from "@/localization";
import { canonicalizeLanguageAlternates } from "@/lib/localized-alternates";
import { buildLocalizedPath, isPublicLocalizedPath } from "@/lib/locale-routing";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import { toWebApiUrl } from "@/lib/webapi-url";

type SeoMetadataInput = {
  culture: string;
  title: string;
  description?: string;
  path: string;
  imageUrl?: string | null;
  noIndex?: boolean;
  type?: "website" | "article";
  allowLanguageAlternates?: boolean;
  languageAlternates?: Record<string, string>;
};

function collapseWhitespace(value: string) {
  return value.replace(/\s+/g, " ").trim();
}

function stripHtml(value: string) {
  return collapseWhitespace(value.replace(/<[^>]+>/g, " "));
}

function truncateText(value: string, maxLength = 160) {
  if (value.length <= maxLength) {
    return value;
  }

  return `${value.slice(0, Math.max(0, maxLength - 3)).trimEnd()}...`;
}

export function getSiteMetadataBase() {
  const { siteUrl } = getSiteRuntimeConfig();
  return new URL(siteUrl);
}

export function buildSeoMetadata({
  culture,
  title,
  description,
  path,
  imageUrl,
  noIndex = false,
  type = "website",
  allowLanguageAlternates = false,
  languageAlternates,
}: SeoMetadataInput): Metadata {
  const shared = getSharedResource(culture);
  const runtimeConfig = getSiteRuntimeConfig();
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  const localizedCanonicalPath = isPublicLocalizedPath(normalizedPath)
    ? buildLocalizedPath(normalizedPath, culture)
    : normalizedPath;
  const resolvedImageUrl = imageUrl ? toWebApiUrl(imageUrl) : undefined;
  const normalizedExplicitAlternates = languageAlternates
    ? canonicalizeLanguageAlternates({
        ...languageAlternates,
        ...(languageAlternates["x-default"]
          ? {}
          : {
              "x-default":
                languageAlternates[runtimeConfig.defaultCulture] ??
                localizedCanonicalPath,
            }),
      })
    : undefined;
  const derivedLanguageAlternates =
    normalizedExplicitAlternates ??
    (allowLanguageAlternates && isPublicLocalizedPath(normalizedPath)
      ? canonicalizeLanguageAlternates({
          "x-default": buildLocalizedPath(
            normalizedPath,
            runtimeConfig.defaultCulture,
          ),
          ...Object.fromEntries(
            runtimeConfig.supportedCultures.map((supportedCulture) => [
              supportedCulture,
              buildLocalizedPath(normalizedPath, supportedCulture),
            ]),
          ),
        })
      : undefined);

  return {
    title,
    description,
    alternates: {
      canonical: localizedCanonicalPath,
      ...(derivedLanguageAlternates
        ? { languages: derivedLanguageAlternates }
        : {}),
    },
    openGraph: {
      type,
      title,
      description,
      url: localizedCanonicalPath,
      siteName: shared.siteTitle,
      locale: culture,
      ...(resolvedImageUrl
        ? {
            images: [
              {
                url: resolvedImageUrl,
                alt: title,
              },
            ],
          }
        : {}),
    },
    twitter: {
      card: resolvedImageUrl ? "summary_large_image" : "summary",
      title,
      description,
      ...(resolvedImageUrl ? { images: [resolvedImageUrl] } : {}),
    },
    ...(noIndex
      ? {
          robots: {
            index: false,
            follow: false,
            googleBot: {
              index: false,
              follow: false,
            },
          },
        }
      : {}),
  };
}

export function buildNoIndexMetadata(
  culture: string,
  title: string,
  description?: string,
  path = "/",
) {
  return buildSeoMetadata({
    culture,
    title,
    description,
    path,
    noIndex: true,
  });
}

export function deriveSeoDescription(
  ...values: Array<string | null | undefined>
) {
  for (const value of values) {
    if (!value) {
      continue;
    }

    const normalized = value.includes("<")
      ? stripHtml(value)
      : collapseWhitespace(value);
    if (normalized) {
      return truncateText(normalized);
    }
  }

  return undefined;
}
