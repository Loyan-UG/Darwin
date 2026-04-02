import type { Metadata } from "next";
import { Fraunces, Manrope } from "next/font/google";
import { SiteShell } from "@/components/shell/site-shell";
import { getShellModel } from "@/features/shell/get-shell-model";
import { getSharedResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { getSiteMetadataBase } from "@/lib/seo";
import "./globals.css";

const bodyFont = Manrope({
  variable: "--font-body-ui",
  subsets: ["latin"],
});

const displayFont = Fraunces({
  variable: "--font-display-ui",
  subsets: ["latin"],
});

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
  };
}

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const shellModel = await getShellModel();

  return (
    <html
      lang={shellModel.culture}
      className={`${bodyFont.variable} ${displayFont.variable} h-full antialiased`}
    >
      <body className="min-h-full bg-[var(--color-surface-canvas)] text-[var(--color-text-primary)]">
        <SiteShell model={shellModel}>{children}</SiteShell>
      </body>
    </html>
  );
}
