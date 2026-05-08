import { cartzillaGroceryTheme } from "@/themes/cartzilla-grocery/theme";
import { harborEditorialTheme } from "@/themes/harbor-editorial/theme";
import { noirBazaarTheme } from "@/themes/noir-bazaar/theme";
import { solsticeMarketTheme } from "@/themes/solstice-market/theme";

export const availableThemes = [
  cartzillaGroceryTheme,
  {
    id: "atelier",
    displayName: "Atelier",
    metadata: {
      title: "Darwin Storefront",
      description:
        "Customer storefront and member portal for shopping, loyalty, orders, and account access.",
    },
  },
  { ...harborEditorialTheme, displayName: "Harbor" },
  { ...noirBazaarTheme, displayName: "Noir" },
  { ...solsticeMarketTheme, displayName: "Solstice" },
] as const;

export type ThemeId = (typeof availableThemes)[number]["id"];

export function resolveTheme(themeId: string) {
  return availableThemes.find((theme) => theme.id === themeId) ?? availableThemes[0];
}
