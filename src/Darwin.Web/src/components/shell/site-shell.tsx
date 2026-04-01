import type { ReactNode } from "react";
import { SiteFooter } from "@/components/shell/site-footer";
import { SiteHeader } from "@/components/shell/site-header";
import type { ShellModel } from "@/features/shell/types";

type SiteShellProps = {
  children: ReactNode;
  model: ShellModel;
};

export function SiteShell({ children, model }: SiteShellProps) {
  return (
    <div className="relative flex min-h-screen flex-col">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-[28rem] bg-[radial-gradient(circle_at_top_right,rgba(62,107,61,0.18),transparent_26rem)]"
      />
      <SiteHeader
        navigation={model.primaryNavigation}
        utilityLinks={model.utilityLinks}
        activeThemeName={model.activeThemeName}
        culture={model.culture}
        supportedCultures={model.supportedCultures}
        menuSource={model.menuSource}
        menuStatus={model.menuStatus}
        menuMessage={model.menuMessage}
      />
      <main className="flex flex-1 flex-col">{children}</main>
      <SiteFooter groups={model.footerGroups} culture={model.culture} />
    </div>
  );
}
