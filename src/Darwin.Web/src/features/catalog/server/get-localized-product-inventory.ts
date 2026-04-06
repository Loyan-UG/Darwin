import "server-only";
import { cache } from "react";
import { getLocalizedPublicDiscoveryInventory } from "@/features/storefront/server/get-localized-public-discovery-inventory";

export const getLocalizedProductInventory = cache(async () => {
  const inventory = await getLocalizedPublicDiscoveryInventory();
  return inventory.products;
});
