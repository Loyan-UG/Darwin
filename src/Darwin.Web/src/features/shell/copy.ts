import type { ShellLink, ShellLinkGroup } from "@/features/shell/types";
import { getShellResource } from "@/localization";

type ShellCopy = {
  shellTagline: string;
  shellBadge: string;
  menuSourceLabel: string;
  cmsFallbackTitle: string;
  footerEyebrow: string;
  footerTitle: string;
  footerDescription: string;
  fallbackPrimaryNavigation: ShellLink[];
  utilityLinks: ShellLink[];
  footerGroups: ShellLinkGroup[];
};

export function getShellCopy(culture: string) {
  return getShellResource(culture) as ShellCopy;
}
