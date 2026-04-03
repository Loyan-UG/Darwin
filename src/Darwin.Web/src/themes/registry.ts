import { cartzillaGroceryTheme } from "@/themes/cartzilla-grocery/theme";
import { harborEditorialTheme } from "@/themes/harbor-editorial/theme";
import { noirBazaarTheme } from "@/themes/noir-bazaar/theme";
import { solsticeMarketTheme } from "@/themes/solstice-market/theme";

export const availableThemes = [
  cartzillaGroceryTheme,
  {
    id: "atelier",
    displayName: "Atelier Journal",
    metadata: {
      title: "Darwin Storefront",
      description:
        "Darwin.Web public storefront and member portal foundation with a theme-isolated, CMS-aware shell.",
    },
  },
  harborEditorialTheme,
  noirBazaarTheme,
  solsticeMarketTheme,
] as const;

export type ThemeId = (typeof availableThemes)[number]["id"];

export function resolveTheme(themeId: string) {
  return availableThemes.find((theme) => theme.id === themeId) ?? availableThemes[0];
}
