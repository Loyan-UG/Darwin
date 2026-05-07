import { SiteHeaderTemplate } from "@/components/shell/site-header-template";
import { getShellCopy } from "@/features/shell/copy";
import type { ShellLink } from "@/features/shell/types";

type SiteHeaderProps = {
  navigation: ShellLink[];
  utilityLinks: ShellLink[];
  culture: string;
  supportedCultures: string[];
  languageAlternates?: Record<string, string>;
};

export function SiteHeader({
  navigation,
  utilityLinks,
  culture,
  supportedCultures,
  languageAlternates,
}: SiteHeaderProps) {
  const copy = getShellCopy(culture);

  return (
    <SiteHeaderTemplate
      navigation={navigation}
      utilityLinks={utilityLinks}
      culture={culture}
      supportedCultures={supportedCultures}
      languageAlternates={languageAlternates}
      brandName="Darwin"
      primaryNavigationLabel={copy.primaryNavigationLabel}
    />
  );
}
