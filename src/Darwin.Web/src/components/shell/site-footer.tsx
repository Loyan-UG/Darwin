import { SiteFooterTemplate } from "@/components/shell/site-footer-template";
import { getShellCopy } from "@/features/shell/copy";
import type { ShellLinkGroup } from "@/features/shell/types";

type SiteFooterProps = {
  groups: ShellLinkGroup[];
  culture: string;
  columnCount: number;
};

export function SiteFooter({ groups, culture, columnCount }: SiteFooterProps) {
  const copy = getShellCopy(culture);

  return (
    <SiteFooterTemplate
      groups={groups}
      culture={culture}
      columnCount={columnCount}
      eyebrow={copy.footerEyebrow}
      title={copy.footerTitle}
      description={copy.footerDescription}
    />
  );
}
