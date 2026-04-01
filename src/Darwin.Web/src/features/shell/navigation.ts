import type { ShellLink, ShellLinkGroup } from "@/features/shell/types";
import { getShellCopy } from "@/features/shell/copy";

export function getFallbackPrimaryNavigation(culture: string): ShellLink[] {
  return getShellCopy(culture).fallbackPrimaryNavigation;
}

export function getUtilityLinks(culture: string): ShellLink[] {
  return getShellCopy(culture).utilityLinks;
}

export function getFallbackFooterGroups(culture: string): ShellLinkGroup[] {
  return getShellCopy(culture).footerGroups;
}
