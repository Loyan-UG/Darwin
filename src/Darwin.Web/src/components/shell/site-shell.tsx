import type { ReactNode } from "react";
import { SiteFooter } from "@/components/shell/site-footer";
import { SiteHeader } from "@/components/shell/site-header";
import type { ShellModel } from "@/features/shell/types";

type SiteShellProps = {
  children: ReactNode;
  model: ShellModel;
  languageAlternates?: Record<string, string>;
};

export function SiteShell({
  children,
  model,
  languageAlternates,
}: SiteShellProps) {
  return (
    <div className="relative flex min-h-screen flex-col">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-[34rem] bg-[radial-gradient(circle_at_top_right,rgba(76,175,80,0.16),transparent_28rem),radial-gradient(circle_at_top_left,rgba(255,152,0,0.12),transparent_24rem)]"
      />
      <SiteHeader
        navigation={model.primaryNavigation}
        utilityLinks={model.utilityLinks}
        culture={model.culture}
        supportedCultures={model.supportedCultures}
        languageAlternates={languageAlternates}
      />
      <main className="flex flex-1 flex-col">{children}</main>
      <SiteFooter
        groups={model.footerGroups}
        culture={model.culture}
        columnCount={model.footerColumnCount}
      />
    </div>
  );
}
