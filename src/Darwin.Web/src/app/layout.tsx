import type { Metadata } from "next";
import { SiteShell } from "@/components/shell/site-shell";
import { getShellModel } from "@/features/shell/get-shell-model";
import { getRequestLanguageAlternates } from "@/features/shell/server/get-request-language-alternates";
import { getSharedResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { getSiteMetadataBase } from "@/lib/seo";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";
import "./globals.css";

export async function generateMetadata(): Promise<Metadata> {
  const culture = await getRequestCulture();
  const shared = getSharedResource(culture);

  return {
    metadataBase: getSiteMetadataBase(),
    title: {
      default: shared.siteTitle,
      template: `%s | ${shared.siteTitle}`,
    },
    description: shared.siteDescription,
    icons: {
      icon: [
        {
          url: "/images/DarwinJustLogo.png",
          type: "image/png",
        },
      ],
      apple: [
        {
          url: "/apple-icon.png",
          type: "image/png",
        },
      ],
    },
  };
}

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const culture = await getRequestCulture();
  const [shellModel, languageAlternates] = await Promise.all([
    getShellModel(culture),
    getRequestLanguageAlternates(),
  ]);
  const runtimeConfig = getSiteRuntimeConfig();

  return (
    <html
      lang={shellModel.culture}
      data-theme={runtimeConfig.theme}
      className="h-full antialiased"
      suppressHydrationWarning
    >
      <body className="min-h-full bg-[var(--color-surface-canvas)] text-[var(--color-text-primary)]">
        <SiteShell model={shellModel} languageAlternates={languageAlternates}>
          {children}
        </SiteShell>
      </body>
    </html>
  );
}



