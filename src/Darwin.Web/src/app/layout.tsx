import type { Metadata } from "next";
import { Fraunces, Manrope } from "next/font/google";
import { SiteShell } from "@/components/shell/site-shell";
import { getShellModel } from "@/features/shell/get-shell-model";
import { activeTheme } from "@/themes/registry";
import "./globals.css";

const bodyFont = Manrope({
  variable: "--font-body-ui",
  subsets: ["latin"],
});

const displayFont = Fraunces({
  variable: "--font-display-ui",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: {
    default: activeTheme.metadata.title,
    template: `%s | ${activeTheme.metadata.title}`,
  },
  description: activeTheme.metadata.description,
};

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
